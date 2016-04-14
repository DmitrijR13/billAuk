delete from map_points where nzp_mo in (select nzp_mo from map_objects where tip in (-1,-2));
delete from map_objects where tip in (-1,-2);
insert into map_objects (tip, note) values (-1, 'ANxfQU0BAAAA-5JTKQIAZqDm6jpY7AMd5oxJ_0cDue0pX9EAAAAAAAAAAACfHJffTds2p14Rtjd4WeeUBjTFGA==');
insert into map_objects (tip, note) values (-2, 'г. Самара');
insert into map_points (nzp_mo, x, y) select nzp_mo, 50.196333, 53.244172 from map_objects where tip = -2;

delete from roles where cur_page in (81) and tip = 2 and kod = 7; 

insert into webdb.roles
select 0, nzp_role, cur_page, 2, nzp_act,''
from actions_show a, s_roles b
where a.cur_page in (81) and a.nzp_act in (7) and b.nzp_role in (10,11);

update webdb.roles set sign = encrypt_aes(tip||kod||cur_page||nzp_role||'-'||nzp_rls||'roles') where cur_page in (81) and tip = 2 and kod = 7; 