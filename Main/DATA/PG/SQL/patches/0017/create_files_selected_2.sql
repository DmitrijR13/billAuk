#public схема 

alter table  calc_fon_0 add column  parameters varchar(255);
alter table  calc_fon_1 add column  parameters varchar(255);
alter table  calc_fon_2 add column  parameters varchar(255);
alter table  calc_fon_3 add column  parameters varchar(255);
alter table  calc_fon_0   alter column  parameters  type   character(2000);
alter table  calc_fon_1   alter column  parameters  type   character(2000);
alter table  calc_fon_2   alter column  parameters  type   character(2000);
alter table  calc_fon_3   alter column  parameters  type   character(2000);