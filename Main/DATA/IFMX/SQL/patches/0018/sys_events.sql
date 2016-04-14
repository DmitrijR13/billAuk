--central data
update sys_dictionary_values set code = 1 where nzp_dict in (8216, 8216, 6481, 6602, 6603, 6604, 6600, 6601);
update sys_dictionary_values set code = 2 where nzp_dict in (6489, 6490, 6491);
update sys_dictionary_values set code = 3 where nzp_dict in (8215, 6484, 6606, 6485, 6608, 6609);
update sys_dictionary_values set code = 4 where nzp_dict in (6492, 6493, 6494, 6607);
update sys_dictionary_values set code = 5 where nzp_dict in (6486, 6487, 6488);
update sys_dictionary_values set code = 6 where nzp_dict in (6611, 6612, 6498, 6499, 6500);
update sys_dictionary_values set code = 7 where nzp_dict in (6597, 6598, 6599);
update sys_dictionary_values set code = 8 where nzp_dict in (7429);

update sys_dictionary_values set (name, code) = ('Расчёт начислений ЛС', 1) where nzp_dict in (6595);

insert into object_types (id, type_name) values (3, 'Дом');
insert into object_types (id, type_name) values (4, 'Жилец');
insert into object_types (id, type_name) values (5, 'Оплата');
insert into object_types (id, type_name) values (6, 'Пачка оплат');
insert into object_types (id, type_name) values (7, 'Перекидка');
insert into object_types (id, type_name) values (8, 'Договор');

insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6496, 'Изменение ПУ', 101, '2');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6497, 'Добавление ПУ', 101, '2');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6498, 'Изменение пачки оплат', 101, '2');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6499, 'Добавление пачки оплат', 101, '2');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6500, 'Удаление пачки оплат', 101, '2');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (7431, 'Удаление договора с квартиросъёмщиком', 101, '8');
insert into sys_dictionary_values (nzp_dict, name, nzp_tdict, code) values (6594, 'Расчёт начислений по дому', 101, '3');

--central fin  
CREATE TABLE "are".sys_events(
   nzp_event SERIAL NOT NULL,
   date_ DATETIME YEAR to FRACTION(3) default Current YEAR to FRACTION(3),
   nzp_user INTEGER,
   nzp_dict_event INTEGER,
   nzp INTEGER,
   note CHAR(200))
EXTENT SIZE 3348 NEXT SIZE 332 LOCK MODE PAGE;