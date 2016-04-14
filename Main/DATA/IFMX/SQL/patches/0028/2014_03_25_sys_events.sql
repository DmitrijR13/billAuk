--central _data
CREATE TABLE "are".actions_types(
   id SERIAL NOT NULL,
   action_name CHAR(25))
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

--central _data
ALTER TABLE "are".sys_dictionary_values ADD action integer;

INSERT INTO "are".sys_dictionary_values(
            nzp_dict, name, nzp_dict_parent, nzp_tdict, code, note)
    VALUES (6613, 'Закрытие пачки оплат',null, 101, 5, null);

insert into actions_types (id, action_name) values (1, 'Системное');
insert into actions_types (id, action_name) values (2, 'Просмотр');  
insert into actions_types (id, action_name) values (3, 'Удаление'); 
insert into actions_types (id, action_name) values (4, 'Добавление');  
insert into actions_types (id, action_name) values (5, 'Изменение');  
insert into actions_types (id, action_name) values (6, 'Открытие');  
insert into actions_types (id, action_name) values (7, 'Закрытие');  
insert into actions_types (id, action_name) values (8, 'Расчет'); 
insert into actions_types (id, action_name) values (9, 'Формирование'); 
insert into actions_types (id, action_name) values (10, 'Отмена'); 
insert into actions_types (id, action_name) values (11, 'Подтверждение'); 
insert into actions_types (id, action_name) values (12, 'Печать'); 

update sys_dictionary_values set action = 1 where nzp_dict in (6479,6480);
update sys_dictionary_values set action = 2 where nzp_dict in (6490,6605,6606,6607);
update sys_dictionary_values set action = 3 where nzp_dict in (6482,6485,6487,6490,6493,6500,6598,6601,6603,7431);
update sys_dictionary_values set action = 4 where nzp_dict in (6481,6484,6486,6489,6492,6497,6499,6597,6600,6602);
update sys_dictionary_values set action = 5 where nzp_dict in (6488,6491,6494,6495,6496,6498,6599,6604,6610,6611,7428,8214,8215,8216);
update sys_dictionary_values set action = 6 where nzp_dict in (6608);
update sys_dictionary_values set action = 7 where nzp_dict in (6483,6609,6613);
update sys_dictionary_values set action = 8 where nzp_dict in (6594,6595,7427);
update sys_dictionary_values set action = 9 where nzp_dict in (7429,7430);
update sys_dictionary_values set action = 10 where nzp_dict in (6612);
update sys_dictionary_values set action = 11 where nzp_dict in (6637);
update sys_dictionary_values set action = 12 where nzp_dict in (6596,7800);


