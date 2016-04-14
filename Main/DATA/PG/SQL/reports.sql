delete from role_actions where nzp_role = 30;
delete from role_pages where nzp_role = 30;
delete from img_lnk where tip = 3 and kod = 30;
delete from s_roles where nzp_role = 30;
delete from pages_show where cur_page = 333 and page_url = 5;
delete from pages_show where cur_page = 5 and page_url = 333;
delete from pages where nzp_page = 333;

insert into pages (nzp_page,up_kod,group_id,page_menu,page_url,page_name,hlp) values (333,75  ,null, 'Отчеты','~/kart/bill/reports.aspx','Отчеты','переходит на форму выполнения отчетов');

insert into pages_show (cur_page,page_url,up_kod,sort_kod) values (333,5,0,5);
insert into pages_show (cur_page,page_url,up_kod,sort_kod) values (5,333,75,75);

insert into s_roles (nzp_role, role, page_url, sort) values (30, 'Отчеты', 333, 11);
insert into img_lnk (cur_page, tip, kod, img_url) values (0, 3, 30, 'folder_home.png');

insert into role_pages (nzp_role, nzp_page, sign) values (30,0,'');

insert into role_pages (nzp_role, nzp_page, sign)
select distinct b.nzp_role,a.cur_page,''
from pages_show a, s_roles b
where a.cur_page in (5,333)
and b.nzp_role = 30;

insert into role_actions(nzp_role, nzp_page, nzp_act, sign, mod_act)
select distinct nzp_role,cur_page,nzp_act,'',cast(null as integer)
from actions_show a, s_roles b
where a.cur_page in (5,333)
and a.nzp_act in (5,69,610)
and b.nzp_role = 30;

update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show'
where (cur_page = 333 and page_url = 5) or (cur_page = 5 and page_url = 333);

update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages'
where nzp_role = 30;

update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role = 30;
