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


-- �������(�). ���������� ��� ����, ����� �������� ����������� �������� �����  
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
-- �� �������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (206, '~/kart/bill/report.aspx', '������', '������', '��������� � ������ �������, ����������� �� ����� ����� ������', 75, 75);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (322, '~/debitor/find/finddebt.aspx', '����� �� �����', '����� �� �����', '���������� ����� ��� ������ �� �������������', 30, 30);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (323, '~/debitor/find/finddeals.aspx', '����� �� �����', '����� �� �����', '���������� ����� ��� ������ �� ����� ���������', 30, 30);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (324, '~/debitor/list/debitors.aspx', '��������', '��������', '���������� ��������� ������ ������� ������ � ���������������', 40, 40);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (325, '~/debitor/list/deals.aspx', '����', '����', '���������� ��������� ������ ��� ���������', 40, 40);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (326, '~/debitor/deal/deal.aspx', '����', '����', '��������� �� ����� ���������� ����', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (327, '~/debitor/deal/agreement.aspx', '����������', '���������� � ��������� �� ������ �����', '��������� �� ����� ���������� ���������� � �������������� ��������� �� ������ �����', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (328, '~/debitor/deal/debtclaim.aspx', '���', '���', '��������� �� ����� ���������� ����', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (329, '~/debitor/deal/debtchange.aspx', '��������� �������������', '��������� �������������', '��������� �� ����� ������������� ����� �����', null, 326);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (330, '~/debitor/settings/debtsettings.aspx', '���������', '���������', '��������� �� ����� �������� ������� ���������', 270, 270);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (332, '~/debitor/operations/dealoperations.aspx', '� ������', '��������� �������� � ������', '��������� �� ����� ��������� �������� � ��������� ������� ���', 74, 325);


--���������� ������� s_actions
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (1, '��������� �����', '��������� ����� ����� ���������� ��������� ����� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (2, '�������� ������', '������� ����� ����������� ���� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (3, '������� ������', '��������� ��������� ������ �� �������� ��� ��� ��������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (4, '�������� ������', '��������� ������ � ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (5, '�������� ������', '��������� ����� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (6, '�������� ������', '������� ����� ����������� ���� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (7, '�������� �����', '���������� ������������� ����� �ndex');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (8, '�� ������', '��������� ����� ��� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (9, '����������', '��������� ��� ������������ ������ � ����������� �� �������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (10, '���������', '��������� ������� ������� ��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (11, '�������������', '������������� ������� ������� ��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (12, '������� ���', '������� ��� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (13, '������� ������', '������� ��� �������� ������ ���������� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (14, '������� ���������', '��������� ����� � ����������� � ���������� ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (15, '�������� ������', '������� ������ ���������� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (16, '���������� ��������', '������������� �������� ��������� � �������� ��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (17, '������� ��������', '������� �������� ��������� � �������� ��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (18, '�������� ������������', '��������� ������������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (19, '������� ������������', '������� ������������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (20, '��� ���������', '���������� ��� ���������: � ������� ����������� �������� � �� ������� ������������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (21, '���������� ������', '��������� ������ �������� ������, ���������� � ������� ������� ������, ����������� � ���� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (22, '������� ������', '��������� ���������� � ���, ��� ������ �� ����������� � ��������� ������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (23, '�������� ������', '��������� ������ ������ � ������������ ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (24, '������� ������� ����', '������� ����� ������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (25, '������� ���', '������� ����� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (26, '������� ������', '������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (61, '��������� ���������', '��������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (64, '������� ��', '������� ������ ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (65, '��������� �������', '��������� ������� �������������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (66, '�������� ������', '��������� ���������� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (67, '�������� ��� ��������', '���������� ��� �������� ������ ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (68, '�������� ������', '��������� ������ ��������� ����������� �����������, ��������, �����, �����������, ������ ������, ������� ������������ ��� ������� ���� ������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (69, '�������� �����', '��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (70, '������', '��������� ������ ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (71, '�������� ������������ ���������', '���������� ������������ ��������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (72, '�������� ��������� ���������', '���������� ��������� ��������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (73, '��������� � Excel', '��������� ������ � ���� ����������� ������ (MS Excel)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (74, '������� � �����', '��������� � ����� ��������� �������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (75, '����� �� ������', '������� �� ������ ��������� �������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (76, '�������� �������', '��������� � ��������� ���� �������� �������� ������, � ��������������� �� ����������� � ������ �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (77, '�������� ���������', '��������� � ��������� ���������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (78, '����������', '���������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (79, '��������', '�������� ������������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (80, '�������� ��', '�������� ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (81, '�������� �����-�����', '�������� �����-�����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (82, '�������� � ������', '�������� ������� ����� � ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (83, '��������� �� ������', '��������� ������� ����� �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (84, '� ������ ������', '������� � ������ ������ �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (85, '������� �����', '������� ����� ����� � ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (86, '������� �����', '������� ����� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (87, '�������� ������', '�������� ����� ������ � �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (88, '��������� ������', '��������� ����������� ���� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (89, '����� ������� ����', '��������� � ������� ������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (90, '������� ������', '������� ��������� ������ �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (91, '�������� �������.', '�������� ������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (92, '������� �����', '������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (93, '������������', '��������� ������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (94, '�������� ������', '��������� ����� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (95, '�������� ������', '������� ��������� ������ ���� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (96, '�������� ������������', '��������� ������ ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (97, '���������', '���������� ������ �����-����� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (98, '�������� ���������', '�������� ������ �����-����� ��� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (99, '������������', '��������� ������������ �� ��������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (100, '��������� �������', '���������� ������� ������������ ������������ �� ������������� �����-�������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (101, '����� �������', '������� ������� ������������ ������������ �� ������������� �����-�������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (102, '�������', '������� ������ �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (103, '�������� ���������', '��������� � ��������� ���������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (104, '��������� ������', '��������� � ��������� ���������� ����������� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (107, '�����������', '��������� � ������������ ����� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (108, '������ ������', '��������� ������ ������ ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (109, '�������� ����. ������', '��������� ��������� ������ ���������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (110, '��������� ���������', '��������� ����������� ���� � ����������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (111, '��������', '���������� ���������, ��������� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (112, '� �����', '���������� ���������, ����������� �� ������������ ����� � ����������� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (113, '������������� �������', '������������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (114, '��������� � ��������', '��������� � ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (115, '������ �� ��������', '��������� ��������� ��������� �� ������ �� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (116, '�������� �������', '�������� ��� ������� �������� ������� ������������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (118, '���������', '��������� ������� ����������� ������ � �������� ������������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (120, '������������', '��������� ������������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (122, '������������� ��', '��������� ��������� ������� ������ �� ���������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (123, '������������� ��', '��������� ��������� �������� ����� �� ���������� ������ ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (124, '�������� �������.', '�������� ������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (125, '������ �������������', '��������� ����� �����, ������������� ������� �� ����� ����� ������, � �������� �� � ������� � ������� "����� ������ �� ������������� ����� �������������"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (126, '������ � �������', '�������� �������� � ������� ������ ������, �� ������� ��������� ��������� �� ������, � ���������� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (127, '������� ���������', '������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (128, '������ ��� ������', '���������� ������ ��� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (129, '�������� ������', '��������� �������� ������������ � ����������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (130, '�������', '��������� �����-����� ��� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (131, '��������� ��������', '��������� �������� �� ����������� �������� ���������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (132, '������� �����', '��������� �������� �� ����������� �������� ���������� ������ � ��������� ��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (133, '����������� �����', '���������� ��������� ������ �� ���� ������� ����� � ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (134, '����������� ����', '���������� ��������� ������ �� ���� ������� ���� � ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (135, '������������� ������', '���������� ���� ������ �� ��������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (136, '��������', '�������� ��������� ������-������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (137, '�������� �����������', '��������� ����������� �� ��������� ����� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (140, '�������', '������� ������ � ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (141, '��������', '��������� ����� ������ �� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (142, '�������� ������', '��������� ����� ������ � ������������ �� ������� ��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (143, '�������� ��������', '��������� ������ ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (145, '��������', '��������� ���������� � �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (146, '�������', '������� ���������� � �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (147, '������������', '��������� ������������� ���� �������������� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (148, '�������� �������.', '��������� ������ ������������� ���� �������������� ��� ���������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (149, '������� ������', '��������� �������� ������� �� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (150, '����� ���������', '������� ����� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (151, '���������', '���������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (152, '�������', '������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (153, '���������', '���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (154, '��������� ���', '��������� ��� � ����������� �������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (155, '������ ���', '��������� ������ ���� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (156, '��������� ��������������', '��������� ����������� �������������� ������ ����� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (157, '������ ��������������', '��������� ����������� �������������� ������ ����� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (158, '�������', '������� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (159, '� �������������', '������ ������� ���������� � ������������� �� ������� ������ ������������ ���� ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (160, '������������', '��������� ������������� ������������ ���� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (161, '������� ��������', '�������� �������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (162, '������� ����������', '������� ���������� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (163, '���������', '��������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (164, '����������', '��������� ������ ���������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (165, '�����������', '������� ����� �������� �������� ������ � �������� ������ �� ������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (166, '���������', '��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (167, '��������� ��-���', '�������� ���������� ���������� ���� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (168, '���� ��������', '��������� ���� �������� ������������ ��� �������� ��� � ������� ������� 2.0');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (169, '��������', '�������� ����� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (170, '���������', '��������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (171, '����������������', '��������� ����������������� ����� �� ���������� �������� ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (172, '������ ��.����. ���', '��������� ������ ������� �������� �������������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (173, '���������', '��������� ���������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (174, '���������', '��������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (175, '�����', '���������� ��������� ��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (176, '�������� �������', '��������� ����� ������� ������� ���������� �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (177, '������ �������', '������������ ������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (178, '��������� �� ��', '��������� � ����� �������� ����� � ���������� �������� ����� �� ������ ������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (179, '����� ��������', '������� �����, ������������� �� � ���������� ����� �������� �������� ����� ����� �������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (180, '������ ��������', '��������� � ����� ��������� ������� �������� �� ��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (181, '������ ������', '��������� ���� � ����������� �������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (182, '������� �����', '������� ����� � �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (184, '���������', '��������� ��� ������� ����� ��������� ����� � ����� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (185, '����� �����', '����� ����� ��� ������� ��������� ��������� ����-������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (186, '������� �����', '������� ����� �� ������� ��������� ��������� ����-������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (187, '��������� ������', '��������� �������� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (188, '���������', '��������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (189, '������� ��������', '������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (190, '���� �� �������', '��������� ����� ��� ����� ��������� �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (192, '������� ����������', '��������� ��������� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (193, '������� ���', '��������� ��������� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (194, '�������� ����', '��������� ����� ��� ��������� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (196, '��������� �����', '��������� ��������� ���������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (197, '������������ ���������', '��������� ����� ���������� ��� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (198, '������� ����', '������� ����� ���� ��� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (199, '������� ����', '������ ������ ���� �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (200, '������� ����', '��������� ���� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (201, '������� ���������', '������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (203, '������� � ����', '������� � ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (204, '������� �� ������������', '������� �� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (205, '������� � ��������', '������� � ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (206, '������� ����', '������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (207, '���������-������� ����', '���������-������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (208, '������� � ������� �����', '������� � ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (209, '������� �� �����������', '������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (210, '������� � �������������', '������� � �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (211, '������� �� ������� �����', '������� �� ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (212, '��������� ��������� �� ������� 5.10', '��������� ��������� �� ������� 5.10');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (213, '��������� ��������� �� ������� 5.20', '��������� ��������� �� ������� 5.20');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (214, '��������� � ����������� �� ����� ����������', '��������� � ����������� �� ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (215, '��������� � ������ � ���������������� ����� �� ����� ����������', '��������� � ������ � ���������������� ����� �� ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (216, '��������� � ����������� �� ����� ����������', '��������� � ����������� �� ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (217, '���������� ������� ���� �__', '���������� ������� ���� �__');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (218, '�������� ������ ������', '�������� ������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (219, '�������� ������ ��������', '�������� ������ ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (220, '�������� � ����������� ������� � ������ � ���������������� �����', '�������� � ����������� ������� � ������ � ���������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (221, '������ ������, ���������� ���������� �� �������� ����', '������ ������, ���������� ���������� �� �������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (222, '������ �������, ��������� ��� ���������� ������������� ��������', '������ �������, ��������� ��� ���������� ������������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (223, '�������� � ����������� �� ����� ���������� (����� ���1)', '�������� � ����������� ���������� �� ����� ���������� �� ����� ���1');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (224, '������ ��������������� ����� ��������', '������ ��������������� ����� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (225, '������ �������', '������������� ������ �������, ����������� �� ���������� ������ �������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (226, '��������� � ������/������ ��������', '��������� � ������/������ ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (227, '������ ��������������� ����� �������', '������ ��������������� ����� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (228, '��������� �� ����������', '��������� ������� �� ���������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (229, '�������� �������������� �����', '����� �������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (230, '������ �������� � �������', '������ �������� � �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (231, '����������� �� ����� - �����', '����������� �� ����� - �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (232, '������� �� ����������� ������������ ����� (����� 2)', '������� �� ����������� ������������ ����� (����� 2)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (233, '������� � ������� �����', '������� � ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (234, '������� �� ����������� ������ ������������ �����', '������� �� ����������� ������ ������������ �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (235, '������� ��� ������������ � ���', '������� ��� ������������ � ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (236, '������� �� �������� ����� (Excel)', '������� �� �������� ����� (Excel)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (237, '�������� �����������', '�������� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (238, '�������� �������', '�������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (239, '������� �� ����������� ������������ ����� (����� 1)', '������� �� ����������� ������������ ����� (����� 1)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (240, '��������� �� �����', '��������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (241, '������������ ��������', '������������ ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (242, '������� � ����� ����������', '������� � ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (243, '������� �� ��������', '������� �� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (244, '������ ����������-������������ ������������ ����� �� ������������ ������', '������ ����������-������������ ������������ ����� �� ������������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (245, '����������  �� �������� � ����������', '����������  �� �������� � ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (246, '��� ������ �� ���������������', '��� ������ �� ���������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (247, '��������� �� �����������', '��������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (248, '������� � ����������� �� ���������� �������� �����', '������� � ����������� �� ���������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (249, '������� ��������� �� ���������� ����������� ��', '������� ��������� �� ���������� ����������� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (250, '����� � ����� �����', '����� � ����� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (251, '����������� �� ����� - ���������', '����������� �� ����� - ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (253, '������ ��������� � ��������� ����� �������������', '������ ��������� � ��������� ����� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (254, '�������� � �������������', '�������� � �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (255, '������� � ���������� ����� �� ����� �����', '������� � ���������� ����� �� ����� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (256, '����������� �� ����� � ����', '����������� �� ����� � ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (257, '��������� ������', '��������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (258, '��������� ������� ����������', '��������� ������� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (259, '������������ ���������', '������������ ��������� ��� ������ ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (260, '�������� ������� ������������ ������������ ������������', '�������� ������� ������������ ������������ ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (261, '�������� �������������� ����� �� ������ "���������� �����"', '����� �������������� ����� �� ������ "���������� �����"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (263, '1.5 ������ ������', '1.5 ������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (264, '2.1 ���������� ������� - ������� �� �������', '2.1 ���������� ������� - ������� �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (265, '2.2 ���������� ������� - ������� �� �����������', '2.2 ���������� ������� - ������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (266, '��������� ��������� ��������� (10.14.3)', '��������� ��������� ��������� (10.14.3)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (267, '��������� ��������� ��������� �� ����������� (10.14.1)', '��������� ��������� ��������� �� ����������� (10.14.1)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (268, '������� ��� ��������������������� ������������', '������� ��� ��������������������� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (269, '������� ���� ��� �����', '������� ���� ��� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (270, '������� �� ������� ������ "���������� �����"', '������� �� ������� ������ "���������� �����"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (271, '������� �� ����������� ������ ������������ ����� �� �����', '������� �� ����������� ������ ������������ ����� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (272, '������� �� ����������� ������ ������������ ����� ������� �� ���', '������� �� ����������� ������ ������������ ����� ������� �� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (273, '3.1 �������� �� ����������� ����� �� �����������', '�������� �� ����������� ����� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (274, '3.2 �������� �� ����������� �����', '�������� �� ����������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (275, '3.3 ���� �� ����������� �����', '���� �� ����������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (276, '2.3 ������ �������-������� �� ��������������', '������ �������-������� �� ��������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (277, '1.4 ������ ������������� �������-������� � ����� �������', '1.4 ������ ������������� �������-������� � ����� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (278, '1.2 ���������� � ����������, ���������� ����', '1.2 ���������� � ����������, ���������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (279, '1.1 ����������, ���������� ����', '1.1 ����������, ���������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (280, '2.4 ���������� ������������� ������, �������� ����', '2.4 ���������� ������������� ������, �������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (281, '1.3.1 ������ ���������, ������������������ ����', '1.3.1 ������ ���������, ������������������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (282, '1.3.2 ������ ���������, ������������������ ����(�����)', '1.3.2 ������ ���������, ������������������ ����(�����)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (283, '������ ������� ��������������', '������ �������, ����������� �� ���������� ������ �������� ������� � ������������ ������ ����� ��� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (284, '�������� �������-2', '�������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (285, '������ ����������� �� ����', '������ ����������� �� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (286, '������ ����������� �� ������', '������ ����������� �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (287, '������� ���������� �� ������������� ����� �� ������', '������� ���������� �� ������������� ����� �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (288, '������ ��������� �� ������� ������', '������ ��������� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (289, '���������� �� �����������', '���������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (290, '��������� ��������� ��������� �� ������� ��� ���� �����������', '��������� ��������� ��������� �� ������� ��� ���� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (291, '������� ��������� ���������� � ����� � ������� �����������', '������� ��������� ���������� � ����� � ������� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (292, '4.16. ������ �������� ����� �� ����� �����', '4.16. ������ �������� ����� �� ����� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (293, '4.18. ����� �� ������� �� ����', '4.18. ����� �� ������� �� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (294, '�������� ������������ �������� ���', '�������� ������������ �������� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (295, '��������������� � �������������', '��������������� � �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (296, '������', '��������� ����� � �������������� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (300, '������� �� �� � �������� ���������� ���', '������� �� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (301, '������� �� ������������2', '������� �� ������������2');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (302, '�������� ������� �������� ��� �����������', '�������� ������� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (303, '�������� � ����� 3.0', '�������� � ����� 3.0');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (304, '�������.���������� ���������� �� �������� �� ��� �� �.�������', '���������� ���������� �� �������� �� ���');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (305, '������ ����������� �� �����', '������ ����������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (306, '������ � ����', '������ � ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (307, '��������� ������� ���������� �� �����', '��������� ������� ���������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (308, '����� ����� �� ����� (���)', '����� ����� �� ����� (���)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (309, '������� �� ����������� ������������ ����� (����� 3)', '������� �� ����������� ������������ ����� (����� 3)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (310, '������� � ���������� ����� �� ����� ����� (����� 2)', '������� � ���������� ����� �� ����� ����� (����� 2)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (311, '���������� ��������� ��������� �����', '���������� ��������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (312, '�������� � ��������� ���������� ������', '�������� � ��������� ���������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (313, '������������� � ����������� �� ����� ����������', '������������� � ����������� �� ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (314, '������� � ����� ����������', '������� � ����� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (315, '������� �� �������� �����', '������� �� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (316, '��������� �� �����������', '��������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (317, '���������� ���������� �� ������� ������', '���������� ���������� �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (318, '���������� ���������� �� �����', '���������� ���������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (319, '���������� ���������� �� ��������', '���������� ���������� �� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (320, '�������� ��������� ��', '�������� ��������� ��');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (321, '�������� ���������� � ����', '�������� ���������� � ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (323, '�������� ���������� � ����� ���������� ������ ���������', '�������� ���������� � ����� ���������� ������ ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (324, '50.1 ��������� ���������', '50.1 ��������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (325, '50.2 ��������� ���������', '50.2 ��������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (327, '������ ������������� ������� ������ � �����', '�������� ������ ������������� ������� ������ � �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (329, '��������� �������', '��������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (330, '������� �� ���������� � ������ �����', '������� �� ���������� � ������ �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (331, '������� � ������', '������� � ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (332, '���������� � �������� ������������������', '���������� � �������� ������������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (333, '���������� � �������������', '���������� � �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (334, '������ ������� ��� ����������', '������ ������� ��� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (335, '������� �� ����� ���������', '������� �� ����� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (336, '������� �� ������� ����� ��� �������', '������� �� ������� ����� ��� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (340, '3.70 ������� ����� �� �����������', '3.70 ������� ����� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (341, '3.71 ������� ����� �� ������������', '3.71 ������� ����� �� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (342, '������� �� ���������', '������� �� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (346, '������ ��������� �� �����', '������ ��������� �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (347, '�������� � ���������', '�������� � ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (348, '������� �� ���������', '������� �� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (349, '���������� �����������', '���������� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (350, '�������� �� ������������ � �������������', '�������� �� ������������ � �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (501, '������ �� �������', '��������� ��� ������ ������, ��������� � ������� ������ �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (502, '������ �� ����������', '��������� ��� ������ ������, ��������� � ������� ������ ������������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (503, '������ �� �����������', '��������� ��� ������ ������, ��������� � ������� ������ ���������� � ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (504, '������ �� �������', '��������� ��� ������ ������, ��������� � ������� ������ �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (505, '������ �� ����������', '��������� ��� ������ ������, ��������� � ������� ������ ��������� �������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (506, '������ ������������', '��������� ��� ������ ������, ��������� � ������� ������ ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (507, '������ ���', '��������� ��� ������ ������, ��������� � ������� ������ �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (508, '������ �� �������', '��������� ��� ������ ������, ��������� � ������� ������ �� ������� � �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (509, '������ �� �������', '��������� ��� ������ ������, ��������� � ������� ������ �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (510, '������ �� �������', '��������� ��� ������ ������, ��������� � ������� ������ �� ������� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (512, '������ �� �������� �������', '��������� ��� ������ ������, ��������� � ������� ������ �� �������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (513, '������ �� ������', '��������� ��� ������ ������, ��������� � ������� ������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (514, '������ �� �����', '��������� ��� ������ ������, ��������� � ������� ������ �� ����� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (520, '�� �������', '��������� ������� ������ � ���������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (521, '�� �������', '��������� ������� ������ � ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (522, '�� �����������', '��������� ������� ������ � ������� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (523, '�� ��������', '��������� ������� ������ ������� ������ ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (528, '���������', '��������� ����������� ��������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (530, '������ �� �����', '���������� ������ �������������, ����������� ������ �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (531, '���������������', '���������� ������ ��������������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (532, '� �������', '���������� ������������������ ��������, ����������� � ������� �� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (533, '�����������', '���������� ������������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (534, '��������', '��������� ������� ����������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (535, '���� ������', '���������� �������� � �������� � ���������� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (540, '������� � ����', '��������� � ����� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (541, '������������ �����', '��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (542, '������', '���������� ���������, ��������� � �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (543, '���������', '��������� ��������� ��������� � ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (544, '������ � ������������', '������ � ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (545, '������� �����', '��������� �������� ���������� ������ � ��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (548, '������� ����', '��������� ������������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (549, '��������� �����', '������������ �� ���������� ������������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (550, '����������������', '�������� ������������ ������, ����������� ���������������, �� ��� ����������� �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (551, '����������� � �����.', '�������� ������ �� ������ � ������ �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (552, '������� �������������', '��������� ������� ���������� ��������, ������ ������ �� �������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (554, '�������� ������', '��������� ������ ��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (555, '�������', '������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (601, '����������� �� ������', '��������� ������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (602, '����������� �� ��', '��������� ������ �� ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (603, '����������� �� �����', '��������� ������ �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (604, '����������� �� ������. ���.', '��������� ������ �� ����������� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (605, '����������� �� ������', '��������� ������ �� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (606, '����������� �� ����������', '��������� ������ �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (607, '����������� �� �����', '��������� ������ �� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (608, '����������� �� ������', '��������� ������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (609, '����������� �� ����� ������������', '��������� ������ �� ����� ������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (610, '�� ��������', '��������� ������ �� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (611, '��� ���������', '��������� ������ ��� ��������������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (612, '����������� �� ���� ���������� ���������', '��������� ������������� �� �������� ���� ���������� ���������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (701, '�������� �� 20 �������', '�������� ������ �� 20 �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (702, '�������� �� 50 �������', '�������� ������ �� 50 �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (703, '�������� �� 100 �������', '�������� ������ �� 100 �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (704, '�������� ��� ������', '�������� ��� ������ �� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (705, '�������� �� 10 �������', '�������� ������ �� 10 �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (721, '����������� �������� / ����� / ���', '���������� ������ � ������� ����������� ��������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (722, '���� ������ / ������� / ����� / ���', '���������� ������ � ������� ������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (723, '����� / ���', '���������� ������ � ������� ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (724, '��������� / ������ / �������', '���������� ������ � ������� ����������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (725, '������ / ��������� / ����������� �������� / ������� ', '���������� ������ � ������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (726, '����������� �������� / ��������� / ������ / �������', '���������� ������ � ������� �� � �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (851, '�������', '');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (852, '�������', '');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (853, '���������', '���������� ������������� ��������� �������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (854, '�����', '���������� ������������� �������� ������� �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (861, '�������� ������', '��������� �������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (862, '������� ������', '��������� ������� ���������� ������ ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (863, '�����', '��������� ����� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (870, '����������� ������', '��������� ������ �������� ���������� � ������� (�������������� ������ �����, ���������� � ��.');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (871, '��������� �.�����', '��������� ������� ��������� ��������� ��������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (875, '����������� ��������', '��������� ������� ���������');

                                  
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (31, 513, 1, 1, 513);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (31, 514, 1, 1, 514);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 69, 0, 0, 69);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 285, 2, 5, 285);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 286, 2, 5, 286);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 287, 2, 5, 287);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 292, 2, 5, 292);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 293, 2, 5, 293);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 303, 2, 5, 303);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 305, 2, 5, 305);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 323, 2, 5, 323);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 347, 2, 5, 347);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 1, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 2, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 501, 1, 1, 501);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 514, 1, 1, 514);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 1, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 2, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 501, 1, 1, 501);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 513, 1, 1, 513);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 1, 3, 0, 1544);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 198, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 200, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (325, 1, 3, 0, 1545);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (325, 200, 0, 0, 200);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 61, 0, 0, 4);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 192, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 193, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 194, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 198, 3, 0, 2092);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 199, 0, 0, 5);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 200, 3, 0, 2093);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 26, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 61, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 192, 3, 0, 2096);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 158, 0, 0, 4);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 170, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 193, 3, 0, 2097);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 197, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 170, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 194, 3, 0, 2098);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 61, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (332, 174, 0, 0, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (206, 69, 206);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (322, 1, 324);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (322, 2, 322);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (323, 1, 325);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (323, 2, 323);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (324, 200, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (324, 198, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (325, 200, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 192, 327);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 193, 328);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 199, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 194, 329);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 61, 326);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES (28, '��������', 31, 14);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES (982, '�������� - ���������', 0, 982);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES (0, 3, 28, 'folder_home.png');

INSERT INTO roleskey (nzp_role, tip, kod) VALUES (28, 105, 982);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 0);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 5);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 31);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 75);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 206);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 322);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 323);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 324);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 325);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 326);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 327);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 328);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 329);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 332);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (982, 330);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 5, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 5, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 513, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 514, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 69, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 347, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 501, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 514, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 501, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 513, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 198, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 325, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 325, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 192, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 193, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 194, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 198, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 199, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 192, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 193, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 197, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 194, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 332, 174, null);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 611, null);

-- �������� ���
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (65, '~/kart/sprav/new_contracts.aspx', '�������� ���', '�������� ���', '��������� �� ����� ��� ��������� � ���������� ���������� �� �������� �������-������������ �����', 73, 73);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (65, 169, 15, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (65, 169, 86);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (65, null, null, 73);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act) VALUES  (942, 65, 169);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 65, 169, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 65);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 65);

--��������� ������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (114, '~/kart/serv/new_availableservices.aspx', '��������� ������', '��������� ������', '��������� �� �������� ��������� �����', 73, 73);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (115, '~/kart/serv/new_availableservice.aspx', '��������� ������', '��������� ������', '��������� �� �������� ��������� ������', null, null);

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

-- �������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (142, '~/kart/serv/new_spisservdom.aspx', '�������� �����', '�������� ����� ����', '��������� �� ������ ����� ��� ���������� ����', 71, 71);

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

--����� ������������

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (143, '~/finances/new_payerrequisites.aspx', '����� ������������', '����� ������������', '��������� � ������ ��������� ������ ���������� �����������', 290, 290);
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

--������ �� �������������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (144, '~/kart/charge/new_distrib_dom_supp.aspx', '������ �� �������������', '������ �� �������������', '��������� �� ����� � ������� � ������������� �����', 72, 72);

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

--�����������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (145, '~/finances/sprav/new_contractors.aspx', '�����������', '�����������', '���������� ���������� ������������', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 610, 610, 2, 1);
--INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 77, 77, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 4, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 5, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 61, 145);
--INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 158, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 77, 28);

INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (145, null, null, 290);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 4, 611);
--INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 611, null);
delete from role_actions where nzp_page=145 and nzp_role=805;
delete from roleskey where nzp_role=15 and tip=105 and kod=805;
delete from s_roles where nzp_role=805;
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 145);
DELETE FROM actions_show WHERE cur_page=145 and nzp_act=158;

--�������������� ��������� ���
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (86, '~/kart/sprav/edit_new_contracts.aspx', '�������������� �������� ���', '�������������� �������� ���', '��������� �� ����� �������������� �������� ���', null, null);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (920, '�������� ���. ��������', '�������� ������� �������� ���������� ��������');
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

--������

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (116, '~/kart/serv/newspisserv.aspx', '�������� �����', '�������� �����', '��������� �� ����� ������� �����', 70, 70);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (117, '~/kart/serv/newspisserv.aspx', '�������� �����', '��������� �������� � ��������', '��������� �� ������ ����� ��� ���������� ��������� �������� ��� ���������� �������� �������', 74, 41);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (118, '~/kart/serv/newspisserv.aspx', '�������� �����', '��������� �������� � �������� ��� ��������� �����', '��������� �� ������ ����� ��� ���������� ��������� �������� ��� �������� ������� ��������� �����', 74, 42);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (119, '~/kart/serv/newserv.aspx', '���������� � ������� �������', '���������� � ������� �������', '��������� �� ������ �������� �������� ����������� � ������ ������� ��� ��������� ������ � �������� �����', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (132, '~/kart/serv/newserv.aspx', '���������� � ������� �������', '��������� �������� � �������', '��������� � ���� ���������� ��� �������� ������� �������� ����������� � ������ ������� ��� ��������� ������ � ��������� ������� ������', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (140, '~/kart/serv/newserv.aspx', '���������� � ������� �������', '��������� �������� � ������� ��� ��������� �����', '��������� � ���� ���������� ��� �������� ������� �������� ����������� � ������ ������� ��� ��������� ������ � ������� ������ ��������� �����', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (141, '~/finances/newpayertransfer_supp.aspx', '������������ ������������', '������������ ������������', '��������� �� ����� ������������ ������������ ������� �� ���������', 235, 235);

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

--���������� ������� page_links
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 2, 2);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 30, 30);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 40, 40);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 72, 72);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 73, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 74, 74);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 75, 75);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 77, 77);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 150, 150);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 160, 160);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 270, 270);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 291, 291);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, 41, null, 41);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 104, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 108, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, 42, null, 42);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, 56, null, 56);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 70, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 70, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 71, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, null, 68);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, 69, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, 81, 81, 81);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, 82, 82, 82);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (175, null, 175, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, 69, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (78, 78, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, 91, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 153, null, 153);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 154, null, 154);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 164, null, 164);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (201, 235, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (353, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (273, 273, null, 273);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 276, null, 276);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 277, null, 277);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (256, 290, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (355, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (279, 276, null, 276);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (221, null, null, 301);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (302, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (302, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (303, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (303, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (316, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (316, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (324, 324, 74, 324);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (325, 325, 74, 325);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (352, 352, 352, 352);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (353, null, 356, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 226, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (33, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (35, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (36, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (37, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (38, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (45, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (49, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (51, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (51, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (52, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (53, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (55, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (55, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (59, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (59, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (64, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (64, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (95, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (95, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (97, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (97, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (98, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (98, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (99, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (99, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (101, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (102, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (103, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (105, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (106, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (107, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (109, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (111, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (111, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (112, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (112, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (122, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (122, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (123, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (123, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (124, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (124, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (126, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (130, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (133, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (133, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (137, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (137, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (151, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (152, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (153, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (154, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (155, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (161, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (163, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (164, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (165, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (165, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (166, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (166, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (167, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (167, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (168, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (169, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (170, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (172, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (172, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (174, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (177, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (177, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (179, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (180, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (181, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (182, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (183, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (184, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (184, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (185, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (185, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (186, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (186, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (188, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (188, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (190, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (190, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (191, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (191, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (192, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (192, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (194, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (195, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (198, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (199, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (200, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (203, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (206, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (210, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (211, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (212, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (213, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (214, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (215, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (216, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (217, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (218, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (219, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (221, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (222, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (223, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (224, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (225, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 4, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 53, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 56, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 42, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 3, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 33, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 82, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 361, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 35, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 278, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 999, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 37, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 1, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 292, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 106, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 5, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 36, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 107, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 50, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 75, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 41, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 72, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 40, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 81, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 195, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 30, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 353, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 340, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 34, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 2, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 31, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 38, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 206, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (227, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (228, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (229, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (230, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (231, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (232, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (233, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (234, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (239, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (240, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (240, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (241, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (242, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (242, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (243, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (244, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (244, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (248, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (249, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (253, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (255, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (256, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (257, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (258, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (259, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (260, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (260, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (261, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (262, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (262, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (265, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (266, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (268, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (269, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (271, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (272, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (273, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (274, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (277, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (278, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (279, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (280, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (281, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (281, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (283, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (284, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (285, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (287, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (288, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (298, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (342, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 5, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 44, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 256, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 4, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 2, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 270, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 999, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 152, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 151, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 77, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 293, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 268, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 155, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 161, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 150, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 3, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 271, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 73, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 1, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 160, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, null, 56);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, null, 246);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 197);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (163, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (194, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 193);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (164, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 202, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 199, null, 198);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (62, null, null, 68);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (65, 65, 65, 65);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (144, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (145, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (65, null, null, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (60, null, null, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 117, null);



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

--�������� �������� ���

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (60, '~/finances/contracts/new_contract_details.aspx', '�������� ���', '�������� ���', '��������� �� �������� ��������� ���', 73, 73);

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

--��������� �������� 146 ������ �������� � �� ���������

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (146, '~/kart/charge/listprovs.aspx', '������ ��������', '������ ��������', '��������� �� ����� ������ ��������', 70, 70);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (922, '���������� ��������', '���������� ��������');

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 922, 'recreate_pack.png');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (146, 922, 922, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (146, 922, 146);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (872, '�������������� ��������', 0, 872);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (872, 146, 922, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 146);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 872);

--������ �������� �� �������� 268 ��������� �����
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 5, 5, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 5, 268);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 268, 5, null);

--��������� ������ � ������� 919 ���������� � ��� ��� ���

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (919, '��������� � ���.���.���', '��������� ������ �� ������� ������� ���.������ � �������');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (258, 919, 919, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (258, 919, 258);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 919, 'panel_window.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 258, 919, null);

--��������� �������� 67 ��������� ��������� �� � �� ������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (67, '~/kart/counter/spisval.aspx', '��������� ��������� ��', '��������� ��������� �������� �����', '��������� �� ������ ��������� ��������� �������� ����� ��� ���������� ����', 71, 71);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (111, '��������', '���������� ���������, ��������� ����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (112, '� �����', '���������� ���������, ����������� �� ������������ ����� � ����������� ��');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 71, 71, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 72, 72, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 111, 111, 2, 4);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 112, 112, 2, 4);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 14, 1633, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (67, 5, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (67, 61, 67);



INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 72, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 71, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 67, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 67, 61, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 67);

--��������� �������� 92 ��������� �� � �� ���������, ��������� � ������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (92, '~/kart/counter/counters.aspx', '��������� ��', '��������� ������� �����', '��������� �� ������ ��������� �������� ����� ��� ���������� ����', 71, 71);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 3, 3, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 64, 64, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 3, 93);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 4, 93);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 5, 92);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 14, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 64, 92);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 64, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 4, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 92, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 92, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 93, 4, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 92);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 92);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 92);

--��������� �������� 93 ��������� ������ ����� � �� ������� �����
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 3, 1630, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 4, 1631, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 14, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 61, 93);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 3, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 93);

--�������� �������� 187 �������������� �� � �� ������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (187, '~/kart/counter/countercard.aspx', '�������������� ��', '�������������� ������ �����', '��������� � �������� ��������������� ������� �����', null, null);

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



--��������������� �������� 82 � ���������� ����� �� ��������
UPDATE pages Set page_menu='��������', page_name='��������', hlp='��������' WHERE nzp_page=82;

--�������� ������������ �������� 168 ������ �� ��������� �������
UPDATE pages Set page_menu='��������� �������', page_name='��������� �������', hlp='��������� �� ����� � ����������� � ���������� �������' WHERE nzp_page=168;

--������� ������ 197 ������������ ��������� �� �� ��������
DELETE FROM actions_show WHERE cur_page=328 and nzp_act=197;

DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=328 and nzp_act=197;

--������� �������� 243 �������� ��������� �� �� �� ��������� � ������� ��
delete from role_pages where nzp_page=243;
delete from role_actions where nzp_page=243;
delete from actions_show where cur_page=243;
delete from actions_lnk where cur_page=243;
delete from pages_show where cur_page=243 OR page_url=243;
delete from pages where nzp_page=243;

--������� ��������� 127 ������ �� ������������� �� �� ���������
delete from actions_show where cur_page=127;
delete from actions_lnk where cur_page=127;
delete from role_pages where nzp_page=127;
delete from role_actions where nzp_page=127;
delete from pages_show where cur_page=127 or page_url=127;
DELETE FROM pages WHERE nzp_page=127; 

-- �������� ��� �������� 310 �������� ���� ������ 
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 185, 185, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 186, 186, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 189, 189, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 185, 310);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 186, 310);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 189, 314);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 189, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 185, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 186, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (978, 310);

--������������. �� ������� ������� ��� �������������� ���������� �/� GKHKPFIVE-9880
Delete from  fbill_data.prm_5 where nzp_prm = 1997;

insert into fbill_data.prm_5 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)
values(0, 1997, '01.01.2004', '01.01.3000','1',1,1,now());

SET SEARCH_PATH TO public;

UPDATE pages Set page_menu='������������� ����������', page_name='������������� ����������', hlp='��������� �� ����� � ����������� � ������������� ����������' WHERE nzp_page=165;


--��������� ������ 18 �������� ������������ � 19 ������� �� ���������� �� �������� 55 ������������ � �� ���� ���������
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (18, '�������� ������������', '��������� ������������ �� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (19, '������� ������������', '������� ������������ �� ������');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (55, 18, 18, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (55, 19, 19, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (55, 18, 55);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (55, 19, 55);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (934, 55, 18, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (934, 55, 19, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (935, 55, 18, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (935, 55, 19, 611);

DELETE FROM role_actions WHERE nzp_role=919 and nzp_page=55 and nzp_act=611;

--��������� �������� 28 ��������� �����������, 29 �������� ����������� � �� �������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (28, '~/finances/sprav/contragentparams.aspx', '��������� �����������', '��������� �����������', '��������� �� �������� ��������� �����������', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (29, '~/kart/prm/prm.aspx', '�������� �����������', '�������� �����������', '��������� �� ����� ��������� � �������������� ���������� ��������� �����������', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 77, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 20, 20, 1, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 67, 67, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 16, 16, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 17, 17, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 67, 67, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 77, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 5, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 61, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 67, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 5, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 16, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 17, 29);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 20, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 67, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 16, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 17, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 28);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 29);

--������� ������ 158 ������� �� �������� 145 �����������
DELETE FROM actions_show WHERE cur_page=145 and nzp_act=158;
delete from actions_lnk where cur_page=145 and nzp_act=158;
delete from role_actions where nzp_page=145 and nzp_act=158;

--������� ������ 168 ���� �������� �� �������� 220 ������������ ������������
delete from public.role_actions where nzp_act=168 and nzp_role in (919,950) and nzp_page=220;

-- ��������� ��� ��������� �� �������� 244 ���������
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (244, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (244, 61, 61, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 244, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 244, 61, 611);

-- �������� ���������� ������� ����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (22, '~/admin/process/preparefirstcalcpeni.aspx', '���������� ������� ����', '���������� ������� ������� ������� ����', '���������� ������� ������� ������� ����', 77, 77);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (905, '����������� ������', '�������������� ������ ������ �������� ����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (22, 905, 905, 0, 0);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 905, 'calc32.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 22, 905, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 22);
-- ��������������
update s_actions set act_name='�� ���������', hlp='��������� ������� ������ � ������� ���������' where nzp_act=522;
update pages set page_menu='������� �����������', page_name='������� �����������' where nzp_page=296;
-- ���������� ������ ������� ������ � �� �������
insert into public.role_pages (nzp_role, nzp_page) values (15,41);

-- �������� ���������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (269, '~/finances/settings/servpriority.aspx', '���������� �����', '������������ ������ ��� ����� �����', '���������� ������ �����, ������� ��������� ��� ������������� ������', 270, 270);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 4, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 61, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 133, 3, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 134, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 26, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 4, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 61, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 26, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 133, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 134, 269);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 269, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 133, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 134, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 611, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 269);
-- �������� 89 ������� ���� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (89, '~/finances/pack/addpack.aspx', '������� ���� �����', '���������� ����� ����� � ������ ������� ����', '���������� ����� ���������� ����� ����� � ������ ������� ����', 77, 77);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (917, '������� ���� �����', '������� ���� �����');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 917, 'add_new.png');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 86, 10, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 92, 20, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 917, 40, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 86, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 92, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 170, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 917, 87);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 86, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 92, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 917, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 89);

-- �������� 14 ������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (14, '~/admin/settings/transfer_homes.aspx', '������� �����', '������� ����� �� ������ ���������� ����� � ������', '������� ����� �� ������ ���������� ����� � ������', 77, 77);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (880, '���������', '������� ���� � ������ ��������� ����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (14, 880, 880, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (14, 880, 14);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 880, 'min_window.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 14, 880, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 14);


-- ����� ��� �������� ����� ����� � �� �������
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (909, '�������. ����', '������������������ ����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (917, '������� ���� �����', '������� ���� �����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 909, 909, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 917, 917, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 909, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 917, 87);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 909, 'add_new.png');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 917, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 909, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 917, null);

-- �������� ������� ���� ����� 
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (87, '~/finances/pack/quickaddpackls.aspx', '������� ���� �����', '������� ���� �����', '������� ���� �����', null, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (916, '����. � ������� �����', '��������� � ��������� �����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (87, 916, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (87, 90, 90, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 917, 40, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (87, 916, 87);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (87, 90, 87);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 917, 87);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 916, 'close_pack.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 87, 916, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 87, 90, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 87);

-- ������ 122 ������������� ��
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (974, '��������� ������� ������', 0, 974);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 974);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (42, 122, 7, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (42, 122, 260);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 122, 'save.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 42, 122, null);
delete from public.role_actions where nzp_role=901 and nzp_page=42 and nzp_act=122;

-- ������� ������ 543 ���������
delete from public.actions_lnk where nzp_act=543;
delete from public.actions_show where nzp_act=543;
delete from public.role_actions where nzp_act=543;
delete from public.s_actions where nzp_act=543;
-- ������� ������� �� ��������������� ������ �������� ��������� ��������� 
delete from public.actions_lnk where nzp_act=72;
delete from public.actions_show where nzp_act=72;
delete from public.role_actions where nzp_act=72;
delete from public.s_actions where nzp_act=72;
-- �������� 24 ������������������ ����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (24, '~/finances/pack/autoaddpackls.aspx', '������������������ ����', '������������������ ����', '������������������ ���� ����� ��� ������ �����-�����', null, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (909, '�������. ����', '������������������ ����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (24, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (24, 158, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 909, 909, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (24, 158, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (24, 170, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 909, 24);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 909, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 909, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 24, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 24, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 24);
 -- �������� 268 ��������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (268, '~/kart/charge/calcmonth.aspx', '��������� �����', '��������� �����', '���������� ���������� � ������� ��������� ������ � ������������� ������� ��������� ���������� ������', 77, 77);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 131, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 132, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 5, 5, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 131, 268);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 132, 268);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 5, 268);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (814, '�������� ����� �������� ������', 0, 814);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 814);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 131, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 5, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (814, 268);

-- �������� ���������� ���������� ����

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (20, '~/kart/charge/disablechargepeni.aspx', '���������� ���������� ����', '���������� ���������� ����', '��������� ���������� ���� �� ���������', 77, 77);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (20, 169, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (20, 158, 2, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (20, 158, 20);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (20, 169, 20);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (898, '���������� ���������� ����', 0, 898);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (898, 20, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (898, 20, 169, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (898, 20);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 898);

-- �������� �������� 363 ����� ������
delete from public.actions_show where cur_page=363;
delete from public.actions_lnk where cur_page=363;
delete from public.role_actions where nzp_page=363;
delete from public.role_pages where nzp_page=363;
delete from public.pages_show where cur_page=363 or page_url=363;
delete from public.pages where nzp_page=363;
delete from public.roleskey where nzp_role=10 and tip=105 and kod=813;
delete from public.s_roles where nzp_role=813;
-- �������� �������� 309 ����� ��
delete from public.actions_show where cur_page=309;
delete from public.actions_lnk where cur_page=309;
delete from public.role_actions where nzp_page=309;
delete from public.role_pages where nzp_page=309;
delete from public.pages_show where cur_page=309 or page_url=309;
delete from public.pages where nzp_page=309;

-- ���������� �� ����������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (361, '~/kart/charge/statcharge_supp.aspx', '���������� �� �����������', '���������� �� �����������', '��������� � ���� �� �������� ������� � �����������', 72, 72);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (164, '����������', '��������� ������ ���������� �� �����������');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (361, 1, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (361, 164, 2, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (361, 1, 361);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (361, 164, 361);
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 164, 'calc32.png');
UPDATE img_lnk Set img_url='calc32.png' WHERE cur_page=0 and tip=2 and kod=164;
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 361, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 361, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 361, 164, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 361);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 361);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 72);

-- 260 ��������� ������� ������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (260, '~/kart/adres/gendomls.aspx', '��������� ������� ������', '��������� ������� ������', '���������� ����� ��� ��������� ������� ������ �� ���������� ����', null, null);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (260, 122, 122, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (260, 611, 123, 2, 1);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (260, 122, 260);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 260, 122, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 260, 611, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (974, 260);
delete from public.role_pages where nzp_role=901 and nzp_page=260;
-- ����� 323 �������� � ����� ��� ������
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (323, '�������� ���������� � ����� ���������� ������ ���������', '�������� ���������� � ����� ���������� ������ ���������');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (206, 323, 323, 2, 5);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 206, 323, null);
INSERT INTO report (nzp_act, name, file_name) VALUES  (323, '�������� ���������� � ����� ���������� ������ ���������', 'UnloadingToKassa.frx');
-- �������� ������ 349 ���������� �����������
delete from public.role_actions where nzp_act=349 and nzp_role=10;

-- �������� �������� 304 ������� ��������
delete from public.actions_show where cur_page=304;
delete from public.actions_lnk where cur_page=304;
delete from public.role_actions where nzp_page=304;
delete from public.role_pages where nzp_page=304;
delete from public.pages_show where cur_page=304 or page_url=304;
delete from public.pages where nzp_page=304;
-- ��������  366 ���������� �����������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (366, '~/finances/operations/overpaymentmanager.aspx', '���������� �����������', '���������� �����������', '��������� �� �������� ���������� �����������', 77, 77);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (926, '�������� �������', '�������� �������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (927, '�������� ���������', '�������� ���������');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 926, 'close_zakaz.png');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 927, 'add_new_ls.png');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 66, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 93, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 170, 8, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 926, 16, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 927, 12, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 93, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 170, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 66, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 926, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 927, 366);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (864, '���������� �����������', 0, 864);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (15, 105, 864);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 66, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 93, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 926, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 927, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (864, 366);
-- ��������� �������� 165 ������������� ����������
Delete from role_actions where nzp_role=901 and nzp_page=165 and nzp_act=178;
delete from role_actions where nzp_role=10 and nzp_page=165 and nzp_act=180;
delete from public.role_actions where nzp_role=901 and nzp_act=158 and nzp_page=165;


-- �������� 347 ������� ������� ����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (347, '~/kart/adres/dom_events.aspx', '�������', '������� ������� ����', '������� ������� ����', 71, 71);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 347);

-- �������� 349 ���������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (349, '~/kart/prm/params.aspx', '���������', '���������', '��������� � ����������� ����������', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 169, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 169, 349);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 158, 349);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 170, 349);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (993, '���������� ����������', 0, 993);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (993, 349);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 993);

-- �������� 21 ���� ������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (21, '~/kart/sprav/typestempdeparture.aspx', '���� ������', '���� ���������� ������ �������', '���� ���������� ������ �������', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (21, 169, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (21, 170, 170, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 21, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 21, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 21);

-- ������� �������� 10 �������� ������� �� ���������
delete from public.actions_show where cur_page=10;
delete from public.actions_lnk where cur_page=10;
delete from public.role_actions where nzp_page=10;
delete from public.role_pages where nzp_page=10;
delete from public.pages_show where cur_page=10 or page_url=10;
delete from public.pages where nzp_page=10;
-- ��������� ������ ������� ��

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (931, '������� ������� ����', '������� ������� ����');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (41, 931, 931, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (41, 931, 41);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 931, 'delete.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 41, 931, 611);


-- ��������� 263 ��������� ���� ������������� �����
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (263, '~/kart/prm/groupprm.aspx', '��������� ���� ������������� �����', '��������� ���� ������������� �����', '���������� ����� ��� ���������� ����� ������������� �����', 74, 41);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (263, 5, 263);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (263, 61, 263);

UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 263);

-- ������ � ����������� �� �������� 134
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 69, 69, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 69, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (247, '��������� �� �����������', '��������� �� �����������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (228, '��������� �� ����������', '��������� ������� �� ���������� �����');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (259, '������������ ���������', '������������ ��������� ��� ������ ������� ������');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (295, '��������������� � �������������', '��������������� � �������������');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 228, 228, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 247, 247, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 259, 259, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 295, 295, 2, 5);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 247, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 228, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 259, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 295, null);

INSERT INTO report (nzp_act, name, file_name) VALUES  (247, '��������� �� �����������', 'web_247.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (228, '��������� �� ����������', 'web_otchet_param.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (259, '������������ ���������', 'Web_259.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (295, '��������������� � �������������', 'web_295.frx');

-- �������� ������ ���� ������ �� ��������� � �� ��������� �� �������� ���������� �����
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (135, 878, 16, 2, 5);

DELETE FROM actions_show WHERE cur_page=133 and nzp_act=878;

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 135, 878, null);



-- ������� �������� 299 ��������� ������
delete from public.actions_show where cur_page=299;
delete from public.actions_lnk where cur_page=299;
delete from public.role_actions where nzp_page=299;
delete from public.role_pages where nzp_page=299;
delete from public.pages_show where cur_page=299 or page_url=299;
delete from public.pages where nzp_page=299;

-- ��������� ������ 915 �������� ������ ��� ��� ������
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (915, '���. ���. ��� ���. ���.', '������� ������ ��� ������������� ������');
update s_actions set act_name='���. ���. ��� ���. ���.' where nzp_act=915;

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (162, 915, 4, 0, 0);

DELETE FROM actions_show WHERE cur_page=162 and nzp_act=4;

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (162, 915, 91);

DELETE FROM actions_lnk WHERE cur_page=162 and nzp_act=4;

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 915, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (904, 162, 915, 611);

DELETE FROM role_actions WHERE nzp_role=904 and nzp_page=162 and nzp_act=4;

-- ����� �� ������ ��� �� �������
delete from public.pages_show where cur_page=31 and page_url=257;
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 31, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 31, 2, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 31);
-- �������� 113 ������� � ������ ����������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (113, '~/admin/files/import_export_param.aspx', '������� � ������ ����������', '������� � ������ ����������', '��������� �� �������� �������� � ������� ����������', 270, 270);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (113, 188, 188, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (113, 163, 163, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (113, 163, 113);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (113, 188, 113);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 113, 188, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 113, 163, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 113);

--���� 946 ��������� - ������������� ������������ ���
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (946, '��������� (������������� ������������ ���)', 0, 946);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 160, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 296, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (946, 220);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 946);
-- �������� ���������� �� ��������� �����������
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (12, '~/exchange/upload/supp_charges.aspx', '�������� ����������', '�������� ���������� ��������� �����������', null, 270, 270);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (12, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (12, 158, 12);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 12, 158, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 12);


--������ �������� ������ ������ ���������� � �� ������� (123 �������� ������ 122)
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

--������� �������� 206 ������ ��  �� ��������
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=69;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=343;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=344;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=345;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=347;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=610;

DELETE FROM role_pages WHERE nzp_role=28 and nzp_page=206;

--������� ����� ������ ��  �� ���������
DELETE FROM role_pages WHERE nzp_role=11 and nzp_page=75;
DELETE from role_pages where nzp_role=11 and nzp_page=206;

-- ������� ������� ������ ���� �� ������ ���
update pages set up_kod=40, group_id=40  where nzp_page=57;
delete from pages_show where cur_page=57 or page_url=57;


INSERT INTO pages_show (cur_page,page_url,up_kod,sort_kod)
SELECT DISTINCT a.nzp_page, b.nzp_page, COALESCE(b.up_kod,0), b.nzp_page
FROM pages a, pages b, page_links  pl
WHERE (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))
and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)
and (select count(*) from pages_show ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page) = 0;

--�������� ����������� ������ ����, � ������� ���� �������
CREATE temp table ps (cur_page integer, up_kod integer);
insert into ps select distinct cur_page, up_kod from pages_show a where up_kod > 0 and not exists (select 1 from pages_show where cur_page = a.cur_page and page_url = a.up_kod);
insert into pages_show (cur_page, page_url, up_kod, sort_kod) select cur_page, ps.up_kod, 0, 0 from ps, pages Where ps.up_kod=pages.nzp_page;
drop table ps;


delete from pages_show where cur_page in (232,355,353,359,95,96,190,104,105,182,183,224,352, 359) or page_url in (232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_show WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_lnk WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_actions WHERE  nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);

                                                                      --���������� ����� ���������� ��� ����������� ����������� ������� ����
                                                                  --���������� ����� ���������� ��� ����������� ����������� ������� ����
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

UPDATE pages_show SET sort_kod =1 WHERE page_url = 168;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 205;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 108;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 126;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 111;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 362;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 41;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 193;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 50;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 170;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 109;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 42;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 49;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 95;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 104;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 190;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 117;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 53;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 182;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 118;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 256;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 56;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 100;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 355;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 352;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 197;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 102;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 60;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 180;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 121;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 99;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 110;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 65;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 189;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 179;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 238;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 196;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 319;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 241;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 354;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 7;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 124;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 264;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 142;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 98;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 184;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 281;
UPDATE pages_show SET sort_kod =16 WHERE page_url = 57;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 116;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 59;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 192;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 240;
UPDATE pages_show SET sort_kod =30 WHERE page_url = 201;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 185;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 51;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 54;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 92;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 63;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 66;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 186;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 62;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 162;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 67;
UPDATE pages_show SET sort_kod =56 WHERE page_url = 177;
UPDATE pages_show SET sort_kod =61 WHERE page_url = 55;
UPDATE pages_show SET sort_kod =62 WHERE page_url = 61;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 191;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 165;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 262;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 167;
UPDATE pages_show SET sort_kod =76 WHERE page_url = 158;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 347;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 122;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 137;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 166;
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
DROP TRIGGER roleskey_add on roleskey; 
DROP FUNCTION add_to_roleskey(); 
DROP TRIGGER s_roles_add on s_roles; 
DROP FUNCTION add_to_s_roles();
DROP TRIGGER report_add on report; 
DROP FUNCTION add_to_report(); 
  



update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update roleskey set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' 
where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000); 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';



--------------------------------����������� ��������� ��� �����-------------------------------------
 drop FUNCTION if exists  public.SetMapCoordinates();
create or Replace function public.SetMapCoordinates()
returns text as 
$BODY$
DECLARE
nzp_mo int;
Begin
execute 'delete from public.map_objects where tip=-2';
execute 'insert into public.map_objects (tip) values(-2) returning nzp_mo ' into nzp_mo;
execute 'insert into  public.map_points (nzp_mo, x, y) values ('||nzp_mo||', 36.608076, 55.092617)';
return '���������';
end;
$BODY$
LANGUAGE plpgsql;
select public.SetMapCoordinates();
drop FUNCTION if exists  public.SetMapCoordinates();

--*****************************************************************************************************
drop function if exists public.table_exists (text, text);
create function public.table_exists(schm text, tbl text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.tables where table_schema = schm and table_name = tbl limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.column_exists (text, text, text);
create function public.column_exists(schm text, tbl text, clmn text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.columns where table_schema = schm and table_name = tbl and column_name = clmn limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_column (text, text, text, text);
create function public.create_column(schm text, tbl text, clmn text, clmn_type text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD ' || clmn || ' ' || clmn_type;
  end if;
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.constraint_exists (text, text, text);
create function public.constraint_exists(schm text, tbl text, cnstr text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM information_schema.table_constraints WHERE table_schema = schm and table_name = tbl and upper(constraint_name) = upper(cnstr) limit 1; 
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_primary_key(text, text, text, text);
drop function if exists public.create_primary_key(text, text, text);
create function public.create_primary_key(schm text, tbl text, clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
 select 1 into cnt
  from information_schema.table_constraints t_c, information_schema.key_column_usage kcu 
  where t_c.constraint_type = 'PRIMARY KEY'
    and t_c.constraint_name = kcu.constraint_name
    and kcu.table_schema = lower(trim(schm)) 
    and t_c.table_schema = kcu.table_schema 
    and kcu.table_name = lower(trim(tbl)) 
    and t_c.table_name = kcu.table_name 
                and kcu.column_name = lower(trim(clmn)) limit 1;

                cnt := coalesce(cnt, 0);
  
  if cnt <= 0 then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || trim(tbl) || '_pkey PRIMARY KEY (' || clmn|| ')';
  end if;
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_foreign_key(text, text, text, text, text, text, text);
drop function if exists public.create_foreign_key(text, text, text, text, text, text);
create function public.create_foreign_key(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
  cnstr text;
BEGIN
  cnstr := 'FK_' || tbl || '_' || clmn;
  if not public.constraint_exists(schm, tbl, cnstr) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || cnstr || ' FOREIGN KEY (' || clmn|| ') REFERENCES ' || ref_schema || '.' || ref_table || ' (' || ref_clmn || ')';
  end if;
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.can_create_fk(text, text, text, text, text, text);
create function public.can_create_fk(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    return '���������';
  end if;
  
  EXECUTE 'select 1 from ' || schm || '.' || tbl || 
    ' where ' || clmn || ' not in (select ' || ref_clmn || ' from ' || ref_schema || '.' || ref_table || ' where ' || ref_clmn || ' is not null) 
      and ' || clmn || ' is not null ' into cnt; 
  cnt := coalesce(cnt, 0);
  
  if (cnt <> 0) then
    insert into public.agent_contract_error (err) values 
    ('� ������� ' || clmn || ' ������� ' || schm || '.' || tbl || ' ���� ��������, ������� ��� � ������� ' || ref_clmn || ' ������� ' || ref_schema || '.' || ref_table);
    return '���������';
  else
    return '���������';  
  end if;
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.index_exists (text, text, text);
create function public.index_exists(schm text, tbl text, indx text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM pg_class c 
   JOIN pg_index i ON i.indexrelid = c.oid
   JOIN pg_class c2 ON i.indrelid = c2.oid
   LEFT JOIN pg_user u ON u.usesysid = c.relowner
   LEFT JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'i' 
   AND n.nspname = schm AND c2.relname = tbl AND upper(c.relname) = upper(indx) limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_index (text, text, text, text);
create function public.create_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_unique_index (text, text, text, text);
create function public.create_unique_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE UNIQUE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;


--*****************************************************************************************************
drop function if exists public.ac_check_fk(text);
create function public.ac_check_fk(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  drop table if exists public.agent_contract_error;
  create table public.agent_contract_error (err text);

  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  perform public.can_create_fk(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  perform public.can_create_fk(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.make_archive(text);
create function public.make_archive(pref text) returns text as 
$BODY$
BEGIN
  if not public.table_exists(pref || '_data', 'fn_bank_old') then
    EXECUTE 'create table ' || pref || '_data.fn_bank_old (LIKE ' || pref || '_data.fn_bank)';
    EXECUTE 'insert into ' || pref || '_data.fn_bank_old select * from ' || pref || '_data.fn_bank';
  end if;

  if not public.table_exists(pref || '_data', 'fn_dogovor_old') then
    EXECUTE 'create table ' || pref || '_data.fn_dogovor_old (LIKE ' || pref || '_data.fn_dogovor)';
    EXECUTE 'insert into ' || pref || '_data.fn_dogovor_old select * from ' || pref || '_data.fn_dogovor';
  end if;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_tables(text);
create function public.ac_create_tables(pref text) returns text as
$BODY$
DECLARE
  sdata text;
  kernel text;
  apref text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  -- fn_scope
  if not public.table_exists (sdata, 'fn_scope') then
    EXECUTE 'set search_path to ''' || sdata || '''';
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_scope_nzp_scope_seq INCREMENT 1 START 1;';
    EXECUTE 'CREATE TABLE fn_scope (nzp_scope integer DEFAULT nextval((''' || sdata || '.fn_scope_nzp_scope_seq''::text)::regclass) NOT NULL) WITH (OIDS=FALSE)'; 
  end if;
  
  -- fn_dogovor_bank_lnk
  if not public.table_exists(sdata, 'fn_dogovor_bank_lnk') then
    EXECUTE 'set search_path to ''' || sdata || '''';
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_dogovor_bank_lnk_id_seq INCREMENT 1 START 1';
    EXECUTE 'CREATE TABLE fn_dogovor_bank_lnk ( 
                  id integer DEFAULT nextval((''' || sdata || '.fn_dogovor_bank_lnk_id_seq''::text)::regclass) NOT NULL,
                  nzp_fd integer NOT NULL,
                  nzp_fb integer NOT NULL,
                  changed_by integer NOT NULL,
                  changed_on timestamp DEFAULT now() NOT NULL) WITH (OIDS=FALSE)';
  end if;

  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'priznak_perechisl', 'integer default 1');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'min_sum', 'numeric(13,2) default 0');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'max_sum', 'numeric(13,2) default 0');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'naznplat', 'varchar(1000)');
  
  -- fn_scope_adres  
  if not public.table_exists(sdata, 'fn_scope_adres') then
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_scope_adres_nzp_scope_adres_seq INCREMENT 1 START 1';
    execute 'CREATE TABLE fn_scope_adres ( 
                  nzp_scope_adres integer DEFAULT nextval((''' || sdata || '.fn_scope_adres_nzp_scope_adres_seq''::text)::regclass) NOT NULL,
                  nzp_scope integer NOT NULL,
                  nzp_wp integer NOT NULL,
                  nzp_town integer,
                  nzp_raj integer,
                  nzp_ul integer,
                  nzp_dom integer) WITH (OIDS=FALSE)';
  end if;
  
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  PERFORM public.create_column(sdata, 'fn_bank', 'note', 'varchar(1000)');
  
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_agent', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_princip', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_scope', 'integer');
  
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_payer_podr', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'dpd', 'smallint DEFAULT 0'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_scope', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'changed_on', 'timestamp DEFAULT now() NOT NULL');

    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN adres_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN phone_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN geton_plat DROP NOT NULL';
  end loop;
  
  -- drop not null
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN num_count DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bank_name DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN kcount DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bik DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN npunkt DROP NOT NULL';

  -- s_payer
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_type DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN changed_on SET DEFAULT now()';

  -- payer_types
  EXECUTE 'alter table ' || kernel || '.payer_types SET WITHOUT OIDS';
  EXECUTE 'alter table ' || kernel || '.payer_types ALTER COLUMN changed_on SET DEFAULT now()';
  
  -- fn_dogovor
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_area DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer_ar DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_fb DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_osnov DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN kpp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN max_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN priznak_perechisl DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN min_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN naznplat DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer DROP NOT NULL';
  
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_indexes(text);
create function public.ac_create_indexes(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_index(kernel, 's_payer', 'ix_s_payer_changed_by', 'changed_by');

  PERFORM public.create_index(kernel, 'payer_types', 'ix_payer_types_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_scope', 'IX_fn_scope_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope', 'ix_fn_scope_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_id', 'id');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fd', 'nzp_fd');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fb', 'nzp_fb');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_changed_by', 'changed_by');

  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer', 'nzp_payer');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer_bank', 'nzp_payer_bank');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_user', 'nzp_user');

  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_agent', 'nzp_payer_agent');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_princip', 'nzp_payer_princip');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_user', 'nzp_user');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_scope', 'nzp_scope');

  PERFORM public.create_unique_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope_adres', 'nzp_scope_adres');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_wp', 'nzp_wp');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_town', 'nzp_town');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_raj', 'nzp_raj');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_ul', 'nzp_ul');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_dom', 'nzp_dom');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'ix_fn_scope_adres_changed_by', 'changed_by');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_agent', 'nzp_payer_agent');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_princip', 'nzp_payer_princip');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_podr', 'nzp_payer_podr');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_supp', 'nzp_payer_supp');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_fn_dogovor_bank_lnk_id', 'fn_dogovor_bank_lnk_id');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_scope', 'nzp_scope');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_changed_by', 'changed_by');
  end loop;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_primary_keys(text);
create function public.ac_create_primary_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_primary_key(sdata, 'users', 'nzp_user'); 
  PERFORM public.create_primary_key(kernel, 's_point', 'nzp_wp');  
  PERFORM public.create_primary_key(sdata, 's_town', 'nzp_town'); 
  PERFORM public.create_primary_key(sdata, 's_rajon', 'nzp_raj'); 
  PERFORM public.create_primary_key(sdata, 's_ulica', 'nzp_ul'); 
  PERFORM public.create_primary_key(sdata, 'dom', 'nzp_dom'); 
  
  PERFORM public.create_primary_key(kernel, 's_payer_types', 'nzp_payer_type'); 
  PERFORM public.create_primary_key(kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_primary_key(kernel, 'payer_types', 'nzp_pt');
  PERFORM public.create_primary_key(sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor_bank_lnk', 'id');
  PERFORM public.create_primary_key(sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor', 'nzp_fd');  
  PERFORM public.create_primary_key(sdata, 'fn_scope_adres', 'nzp_scope_adres');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_primary_key(apref || '_kernel', 'supplier', 'nzp_supp');
  end loop;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_foreign_keys(text);
create function public.ac_create_foreign_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_foreign_key(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_prepare_data(text);
create function public.ac_prepare_data(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  cnt integer;
  apref text;
  anzp_fd integer;
  anzp_supp integer;
  anzp_wp integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';

  -- ��� ���������
  EXECUTE 'select count(*) from ' || kernel || '.s_payer_types where nzp_payer_type = 11' into cnt;
  if (cnt = 0) then
    EXECUTE 'insert into ' || kernel || '.s_payer_types (nzp_payer_type, type_name, is_system) values (11, ''���������'', 1)'; 
  end if;
  
  -- ���������� ��� � ��������� �����
  EXECUTE 'update ' || kernel || '.s_payer s set 
      bik = (select max(trim(bik))   from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank), 
      ks = (select max(trim(kcount)) from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank)
    where s.nzp_payer in (select nzp_payer_bank from ' || sdata || '.fn_bank)';
  
  -- ��� ������ ���������� ����
  EXECUTE 'insert into ' || kernel || '.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on) 
   select distinct nzp_payer, 8, 1, now() from ' || sdata || '.fn_bank where nzp_payer not in (select nzp_payer from ' || kernel || '.payer_types where nzp_payer_type = 8)';
  
  -- ����������
execute 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_princip, 10 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=10 and s.nzp_payer_princip=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_princip,0) > 0';

-- ������
execute 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_agent, 5 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=5 and s.nzp_payer_agent=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_agent,0) >0';

--����������
 EXECUTE 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_supp, 2 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=2 and s.nzp_payer_supp=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_supp,0) >0';

  -- fn_bank
  EXECUTE 'select count(*) from ' || kernel || '.s_payer where nzp_payer = -999999999' into cnt;
  if (cnt = 0) then
    EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) values (-999999999, ''���� �� ���������'', ''���� �� ���������'')'; 
  end if;
  
  execute 'insert into '||sdata||'.fn_bank (nzp_payer,num_count, bank_name, rcount, kcount, bik,nzp_user, dat_when, nzp_payer_bank)
select distinct nzp_payer_princip, 0, ''���� �� ���������'', ''00000000000000000000'',''00000000000000000000'',''000000000'', 1, now(), -999999999
from '||kernel||'.supplier s 
where not exists (select 1 from '||sdata||'.fn_bank b where s.nzp_payer_princip=b.nzp_payer) 
 and s.nzp_payer_princip is not null
ORDER BY nzp_payer_princip';
 
  -- fn_dogovor
  -- nzp_payer_agent
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_agent = (select max(s.nzp_payer_agent) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_agent is not null) 
                where d.nzp_payer_agent is null';
  
  -- nzp_payer_princip
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_princip = (select max(s.nzp_payer_princip) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_princip is not null) 
                where d.nzp_payer_princip is null';

  -- ���������� ������� � �����������              
  EXECUTE 'insert into ' || sdata || '.fn_dogovor (nzp_payer_agent, nzp_payer_princip, nzp_supp, nzp_user, dat_when)
    select nzp_payer_agent, nzp_payer_princip, max(s.nzp_supp), 1, now()
    from ' || kernel || '.supplier s
    where s.nzp_payer_princip is not null 
                  and not exists (select 1 from ' || sdata || '.fn_dogovor d
                    where d.nzp_payer_agent = s.nzp_payer_agent and d.nzp_payer_princip = s.nzp_payer_princip and s.nzp_payer_agent is not null and s.nzp_payer_princip is not null)
                group by 1,2';
  
  
  -- ���������� ������� �������� ��������� ���
  execute 'update ' || sdata || '.fn_dogovor set nzp_scope = null';
  EXECUTE 'set search_path to ' || kernel;  
  for apref in 
     select trim(bd_kernel)p from s_point order by 1
  loop
    execute 'update ' || apref || '_kernel.supplier set nzp_scope = null;';  
  end loop;
  
  execute 'delete from ' || sdata || '.fn_scope_adres';
  execute 'delete from ' || sdata || '.fn_scope';
  
  drop table if exists tmp_supp_wp;
  Create temp table tmp_supp_wp (nzp_supp integer, nzp_wp integer, nzp_scope integer);

  EXECUTE 'set search_path to ' || kernel;  
  for apref, anzp_wp in 
     select trim(bd_kernel), nzp_wp from s_point where flag > 1 order by 1
  loop
      execute 'insert into tmp_supp_wp (nzp_wp, nzp_supp) 
         select distinct '||anzp_wp||', nzp_supp from  '|| apref || '_data.tarif t 
         where coalesce(nzp_supp,0)> 0 and is_actual = 1';
  end loop;
   
  EXECUTE 'select max(nzp_scope) from ' || sdata || '.fn_scope' into cnt ;
  cnt := coalesce(cnt, 1);  
  
  EXECUTE 'set search_path to ' || sdata;  
  for anzp_supp in 
    select distinct nzp_supp from tmp_supp_wp
  loop
    cnt := cnt + 1;
    insert into fn_scope(nzp_scope, changed_by, changed_on) values (cnt, 1, now());
    update tmp_supp_wp set nzp_scope = cnt where nzp_supp = anzp_supp;	
  end loop;
  EXECUTE 'ALTER SEQUENCE fn_scope_nzp_scope_seq RESTART with ' || cnt;  
  
  insert into fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on)  select distinct nzp_scope, nzp_wp, 1, now() from tmp_supp_wp;

  -- ���������� ������� �������� ��������� ���
  EXECUTE 'select max(nzp_scope) from ' || sdata || '.fn_scope' into cnt ;
  cnt := coalesce(cnt, 1);  
  EXECUTE 'set search_path to ' || sdata;  
  
  for anzp_fd in 
    select nzp_fd from fn_dogovor 
    where nzp_payer_agent is not null and nzp_payer_princip is not null
  loop
    cnt := cnt + 1;
    insert into fn_scope(nzp_scope, changed_by, changed_on) values (cnt, 1, now());
    update fn_dogovor set nzp_scope = cnt where nzp_fd = anzp_fd;
  end loop;
  EXECUTE 'ALTER SEQUENCE fn_scope_nzp_scope_seq RESTART with ' || cnt;
  
  execute 'insert into ' || sdata || '.fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on) 
  select distinct d.nzp_scope, t.nzp_wp, 1, now()
  from ' || sdata || '.fn_dogovor d, ' || kernel || '.supplier s, tmp_supp_wp t
  where d.nzp_payer_agent = s.nzp_payer_agent 
    and d.nzp_payer_princip = s.nzp_payer_princip 
    and t.nzp_supp = s.nzp_supp '; 
    
  -- fn_dogovor_bank_lnk
  EXECUTE 'set search_path to ' || sdata; 
  insert into fn_dogovor_bank_lnk (nzp_fd, nzp_fb, changed_by, changed_on)
    select a.nzp_fd, max(b.nzp_fb), 1, now() 
  from fn_dogovor a, fn_bank b 
    where b.nzp_payer = a.nzp_payer_princip and not exists (select 1 from fn_dogovor_bank_lnk l where l.nzp_fd = a.nzp_fd and l.nzp_fb = b.nzp_fb)
  group by 1;
  
  -- supplier
  EXECUTE 'set search_path to ' || kernel; 
  for apref in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'update ' || apref || '_kernel.supplier s set 
     fn_dogovor_bank_lnk_id = (select max(l.id)
     from ' || sdata || '.fn_dogovor d, ' || sdata || '.fn_dogovor_bank_lnk l
       where s.nzp_payer_agent = d.nzp_payer_agent
         and s.nzp_payer_princip = d.nzp_payer_princip
         and d.nzp_fd = l.nzp_fd)
     where s.fn_dogovor_bank_lnk_id is null';
				
     EXECUTE 'update ' || apref || '_kernel.supplier s set 
       nzp_scope = (select max(l.nzp_scope) from tmp_supp_wp l where s.nzp_supp = l.nzp_supp) 
       where s.nzp_scope is null';			
  end loop;

  EXECUTE 'update ' || sdata || '.fn_dogovor_bank_lnk  f set naznplat = (select max(naznplat) from ' || sdata || '.fn_dogovor where nzp_fd = f.nzp_fd)';

  return '���������'; 
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.before_check_fk(text);
create function public.before_check_fk(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  apref text;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';

  -- ��������� ������ ������ � payer_types
  execute 'delete from ' || kernel || '.payer_types where nzp_payer not in (select nzp_payer from ' || kernel || '.s_payer)';
  execute 'delete from ' || kernel || '.payer_types where nzp_payer_type not in (select nzp_payer_type from ' || kernel || '.s_payer_types)';
  
  -- ��������� ������ ������ � fn_bank
  execute 'delete from ' || sdata || '.fn_bank where nzp_payer      not in (select nzp_payer from ' || kernel || '.s_payer)';
  execute 'delete from ' || sdata || '.fn_bank where nzp_payer_bank not in (select nzp_payer from ' || kernel || '.s_payer)';
  
  -- �������� ������������ �� supplier
  EXECUTE 'set search_path to ' || kernel; 
  for apref in select trim(bd_kernel) from s_point order by 1 
  loop
	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_agent, ''��������� �� ��������� '' || nzp_payer_agent, ''��������� �� ��������� '' || nzp_payer_agent 
	  from ' || apref || '_kernel.supplier where nzp_payer_agent not in (select nzp_payer from ' || kernel || '.s_payer)'; 
	
	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_princip, ''��������� �� ��������� '' || nzp_payer_princip, ''��������� �� ��������� '' || nzp_payer_princip 
	  from ' || apref || '_kernel.supplier where nzp_payer_princip not in (select nzp_payer from ' || kernel || '.s_payer)'; 

	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_supp, ''��������� �� ��������� '' || nzp_payer_supp, ''��������� �� ��������� '' || nzp_payer_supp 
	  from ' || apref || '_kernel.supplier where nzp_payer_supp not in (select nzp_payer from ' || kernel || '.s_payer)'; 
  end loop;

  return '���������';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.agent_fin(text, integer, integer);
create function public.agent_fin(pref text, in_year_from integer, in_year_to integer) returns text as
$BODY$
DECLARE
  fin text;
  sdata text;
  i integer;
BEGIN
  i := in_year_from;
  sdata := pref || '_data';
  
  while i <= in_year_to loop
    fin := pref || '_fin_' || i; 
    execute 'alter table ' || fin || '.fn_sended_dom alter column nzp_fd DROP NOT NULL';
    PERFORM public.create_column(fin, 'fn_sended_dom', 'fn_dogovor_bank_lnk_id', 'integer');

    PERFORM public.create_column(fin, 'fn_sended', 'naznplat', 'varchar(1000)');
    PERFORM public.create_column(fin, 'fn_sended', 'fn_dogovor_bank_lnk_id', 'integer');
    PERFORM public.create_column(fin, 'fn_sended', 'nzp_fd', 'integer');
	
    execute 'update ' || fin || '.fn_sended f set naznplat = (select max(a.naznplat) from ' || sdata || '.fn_dogovor a where a.nzp_fd = f.nzp_fd)';
    i := i + 1;
  end loop;
  
  return '���������';
END;
$BODY$
LANGUAGE plpgsql;
--*****************************************************************************************************

drop function if exists public.agent_contract(text);
drop function if exists public.agent_contract(text, integer, integer);

create function public.agent_contract(pref text, in_year_from integer, in_year_to integer) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  aerr text;
  cnt integer;
  anzp_fd integer;
  localbank text;
  is_tula integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';
  
  -- ��������� ������ ������
  PERFORM public.before_check_fk(pref); 
  
  PERFORM public.ac_check_fk(pref);
  
  select count(*) into cnt from public.agent_contract_error;
  
  if  cnt <> 0 then
    RAISE NOTICE '������� � ������';

    for aerr in select err from public.agent_contract_error
    loop
      RAISE NOTICE '%', aerr;
    end loop;
    drop table if exists public.agent_contract_error;

    return '�� ���������. ��. ���������';       
  end if;

  perform public.agent_fin(pref, in_year_from, in_year_to);
  
  -- ������� ����� ��� ������ fn_bank � fn_dogovor
  RAISE NOTICE '�������� ������ ������ fn_bank � fn_dogovor'; 
  perform public.make_archive(pref);
  RAISE NOTICE '����� ������ fn_bank � fn_dogovor ������'; 
  
  RAISE NOTICE '�������� ������'; 
  PERFORM public.ac_create_tables(pref);
  RAISE NOTICE '������� �������'; 

  RAISE NOTICE '�������� ��������'; 
  PERFORM public.ac_create_indexes(pref);
  RAISE NOTICE '������� �������'; 

  RAISE NOTICE '�������� ��������� ������'; 
  PERFORM public.ac_create_primary_keys(pref);
  RAISE NOTICE '��������� ����� �������'; 

  RAISE NOTICE '�������� ������� ������'; 
  PERFORM public.ac_create_foreign_keys(pref);
  RAISE NOTICE '������� ����� �������'; 

  EXECUTE 'drop function if exists ' || kernel || '.trigger_supplier() CASCADE;';

  RAISE NOTICE '���������� ������'; 
  PERFORM public.ac_prepare_data(pref);
  RAISE NOTICE '������ ���������'; 
  
  RAISE NOTICE '�������� ���������'; 
  
  EXECUTE 'CREATE function ' || kernel || '.trigger_supplier() RETURNS trigger AS 
$trigger_supplier$
DECLARE
  fn_dogovor_nzp_payer_agent integer;
  fn_dogovor_nzp_payer_princip integer;
BEGIN
  select nzp_payer_agent, nzp_payer_princip into fn_dogovor_nzp_payer_agent, fn_dogovor_nzp_payer_princip 
  from ' || sdata || '.fn_dogovor_bank_lnk l, ' || sdata || '.fn_dogovor d
  where l.nzp_fd = d.nzp_fd
    and l.id = NEW.fn_dogovor_bank_lnk_id;

  if NEW.nzp_payer_agent <> fn_dogovor_nzp_payer_agent 
    and NEW.nzp_payer_agent > 0         and fn_dogovor_nzp_payer_agent > 0
    and NEW.nzp_payer_agent is not null and fn_dogovor_nzp_payer_agent is not null
  then
    RAISE EXCEPTION ''��������� ����� �������� ��� �� ������������� ���������� ������ �������� ���'';
  end if;

  if NEW.nzp_payer_princip <> fn_dogovor_nzp_payer_princip 
    and NEW.nzp_payer_princip > 0          and fn_dogovor_nzp_payer_princip > 0
    and NEW.nzp_payer_princip is not null  and fn_dogovor_nzp_payer_princip is not null
  then
    RAISE EXCEPTION ''��������� �������� ��� �� ������������� ���������� �������� ���'';
  end if;  

  return NEW;
END;
$trigger_supplier$
LANGUAGE  plpgsql;';

  EXECUTE 'set search_path to ' || kernel; 
  for localbank in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'CREATE TRIGGER ins_supplier BEFORE INSERT ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
    EXECUTE 'CREATE TRIGGER upd_supplier BEFORE UPDATE ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
  end loop;

  RAISE NOTICE '�������� �������';

  return ' ';
END;
$BODY$
LANGUAGE plpgsql;

-- ������� �����
select public.agent_contract('fbill', 13, 15);

drop table if exists fbill_data.nzpfd_general;
drop table if exists fbill_data.nzpfd_good;
-- ����� ��������� ������� ��� ����� ����� 
create table fbill_data.nzpfd_general (
nzp_fd integer,
nzp_fd_del integer,
dat_dog timestamp,
num_dog char(100),
nzp_payer_agent int,
nzp_fb integer,
nzp_payer_princip int);
-- ��������� ������� ��� nzp_fd, ������� ����������
create table fbill_data.nzpfd_good (
nzp_fd integer,
nzp_fd_del integer,
dat_dog timestamp,
num_dog char(100),
nzp_payer_agent int,
nzp_payer_princip int);
-- ������� � ������� fbill_data.nzpfd_general �� nzp_fd, ������� ����� �������
insert into fbill_data.nzpfd_general (nzp_fd_del, dat_dog, num_dog, nzp_payer_agent, nzp_payer_princip)
select nzp_fd, dat_dog,num_dog, nzp_payer_agent, nzp_payer_princip  from fbill_data.fn_dogovor 
where nzp_fd not in (select min(nzp_fd) from fbill_data.fn_dogovor group by nzp_payer_agent ,nzp_payer_princip, dat_dog, num_dog)
order by 4,5;
-- ������� � ������� fbill_data.nzpfd_good �� nzp_fd, ������� ���������� 
insert into fbill_data.nzpfd_good (nzp_fd, dat_dog, num_dog, nzp_payer_agent, nzp_payer_princip)
select nzp_fd, dat_dog,num_dog, nzp_payer_agent, nzp_payer_princip 
from fbill_data.fn_dogovor where nzp_fd  in (select min(nzp_fd) from fbill_data.fn_dogovor group by nzp_payer_agent ,nzp_payer_princip, dat_dog, num_dog)
order by 4,5;
-- ������� � ������� fbill_data.nzpfd_general �� nzp_fd, ������� ���������
update fbill_data.nzpfd_general t set nzp_fd= (select nzp_fd from fbill_data.nzpfd_good f 
 where t.nzp_payer_agent=f.nzp_payer_agent and t.nzp_payer_princip=f.nzp_payer_princip and coalesce(t.num_dog,'x')=coalesce(f.num_dog,'x') and 
 coalesce(t.dat_dog,'2015-06-25')=coalesce(f.dat_dog,'2015-06-25'));
-- ��������� ��� ����������
select * from fbill_data.nzpfd_general;
select * from fbill_data.nzpfd_good order by nzp_fd;

-- ��������� �� �� ��������, ������� ����� �������
select * from fbill_data.fn_dogovor  where nzp_fd in ( select nzp_fd_del from fbill_data.nzpfd_general order by nzp_fd, nzp_fd_del);

--������� � fbill_data.fn_dogovor �� ��������, � ������� nzp_fb = -1      -- � ������� ����� ���������
update fbill_data.fn_dogovor f set nzp_fb = (select max(nzp_fb) from fbill_data.fn_bank where f.nzp_payer_princip = nzp_payer)
where nzp_fb = -1; --and nzp_fd in ( select nzp_fd_del from fbill_data.nzpfd_general order by nzp_fd, nzp_fd_del)

-- �������� ��� fn_sended_dom � fn_sended
drop FUNCTION if exists public.updateFnSended();
create or Replace function public.updateFnSended()
returns text as 
$BODY$
DECLARE
isExists boolean;
schem text;
table_name text;
req text;
begin
FOR i IN 11..15 LOOP
schem='fbill_fin_'||i;
-- ���������� fn_sended
req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended'' and table_schema='''||schem||''')';
EXECUTE req into isExists;
if isExists  then
-- ����������  fn_dogovor_bank_lnk_id
--RAISE NOTICE '%',req;
	req='update '||schem||'.fn_sended s set fn_dogovor_bank_lnk_id = 
(select max(id) from fbill_data.fn_dogovor_bank_lnk l where 
s.nzp_fd = l.nzp_fd and l.nzp_fb in (select nzp_fb from fbill_data.fn_dogovor f  where l.nzp_fd = f.nzp_fd))
where fn_dogovor_bank_lnk_id is not null';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;

-- ���������� fn_sended_dom
	req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended_dom'' and table_schema='''||schem||''')';
	EXECUTE req into isExists;
if isExists  then
	--RAISE NOTICE '%',req;
	req='update '||schem||'.fn_sended_dom d set fn_dogovor_bank_lnk_id= (select fn_dogovor_bank_lnk_id from '||schem||'.fn_sended s where s.nzp_snd=d.nzp_send)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;
END LOOP;
  return ''; 
end;
$BODY$
LANGUAGE plpgsql;
select * from public.updateFnSended();
drop FUNCTION if exists public.updateFnSended();

-- �������� fn_dogovor_bank_lnk_id
update fbill_data.fn_dogovor_bank_lnk l set nzp_fd = (select nzp_fd from fbill_data.nzpfd_general where nzp_fd_del = l.nzp_fd)
where nzp_fd in (select nzp_fd_del from fbill_data.nzpfd_general);


-- �������� ��� fn_sended_dom � fn_sended
drop FUNCTION if exists public.updateFnSendedDom();
create or Replace function public.updateFnSendedDom()
returns text as 
$BODY$
DECLARE
isExists boolean;
schem text;
table_name text;
req text;
begin
FOR i IN 11..15 LOOP
schem='fbill_fin_'||i;
-- ���������� fn_sended
req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended'' and table_schema='''||schem||''')';
EXECUTE req into isExists;
if isExists  then
	-- ���������� nzp_fd
	req='update '||schem||'.fn_sended s set nzp_fd=(select nzp_fd from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)
where exists (select 1 from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;

-- ���������� fn_sended_dom
	req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended_dom'' and table_schema='''||schem||''')';
	EXECUTE req into isExists;
if isExists  then
-- ���������� nzp_fd
	req='update '||schem||'.fn_sended_dom s set nzp_fd=(select nzp_fd from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)
	where exists (select 1 from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;
END LOOP;
  return ''; 
end;
$BODY$
LANGUAGE plpgsql;
select * from public.updateFnSendedDom();
drop FUNCTION if exists public.updateFnSendedDom();

-- ������� ��������� 
delete from fbill_data.fn_dogovor where nzp_fd in (select nzp_fd_del from fbill_data.nzpfd_general);

drop table if exists fbill_data.nzpfd_general;
drop table if exists fbill_data.nzpfd_good;

--SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE   Column_Name = 'nzp_fd'

INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1985', '������ ������', NULL, NULL, NULL, '8', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1985);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1986', '������ ������', NULL, NULL, NULL, '6', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1986);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1987', '������ ������', NULL, NULL, NULL, '2', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1987);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1988', '������ ������', NULL, NULL, NULL, '5', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1988);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1989', '������ ������', NULL, NULL, NULL, '4', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1989);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1990', '������ ������', NULL, NULL, NULL, '1', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1990);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1991', '������ ������', NULL, NULL, NULL, '3', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1991);



select '���������' as ���������;