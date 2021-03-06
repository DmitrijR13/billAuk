-- local _data
CREATE TABLE "are".sys_events(
   nzp_event SERIAL NOT NULL,
   date_ DATETIME YEAR to FRACTION(3) default Current YEAR to FRACTION(3),
   nzp_user INTEGER,
   nzp_dict_event INTEGER,
   nzp INTEGER,
   note CHAR(200))
EXTENT SIZE 3348 NEXT SIZE 332 LOCK MODE PAGE;


--central _data
CREATE TABLE "are".object_types(
   id SERIAL NOT NULL,
   type_name CHAR(25))
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

insert into object_types (id, type_name) values (1, 'ЛС'); 
insert into object_types (id, type_name) values (2, 'ПУ');  

update sys_dictionary_values set code = 1 where nzp_dict in (6481, 6482, 6483, 8214, 6495, 6605, 6637);
update sys_dictionary_values set code = 2 where nzp_dict in (6489, 6490, 6491);

