database websmr;

set encryption password "IfmxPwd2";

delete from userp where nzp_user >= 3 and nzp_user  < 100;
delete from users where nzp_user >= 3 and nzp_user  < 100;

insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (3, 'mrr', 'mrr', 'Мангушев Р.Р.', 'mrr@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (4, 'aidar', 'aidar', 'Айдар', 'aidar@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (5, 'alex', 'alex', 'Алексей', 'alex@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (6, 'albert', 'albert', 'Альберт', 'albert@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (7, 'and', 'and', 'Андрей Зыкин', 'and@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (8, 'kand', 'kand', 'Андрей Кайнов', 'kand@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (9, 'elya', 'elya', 'Эльмира', 'elya@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (10, 'andrey', 'andrey', 'Андрей Шульгин', 'andrey@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (11, 'anes', 'anes', 'Анэс', 'anes@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (12, 'valya', 'valya', 'Валентина', 'valya@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (13, 'dina', 'dina', 'Дина', 'dina@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (14, 'ildar', 'ildar', 'Ильдар', 'ildar@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (15, 'marat', 'marat', 'Марат', 'marat@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (16, 'olleg', 'olleg', 'Олег', 'olleg@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (17, 'olga', 'olga', 'Ольга', 'olga@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (18, 'rust', 'rust', 'Рустем', 'rust@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (19, 'sergey', 'sergey', 'Сергей', 'sergey@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (20, 'diana', 'diana', 'Диана', 'diana@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (21, 'ksu', 'ksu', 'Ксения', 'ksu@stcline.ru', 0);
insert into users (nzp_user, login, pwd, uname, email, is_blocked) values (8, 'kate', 'kate', 'Катя', 'kate@stcline.ru', 0);
                                                                           
update users set pwd = encrypt_aes(nzp_user||'-'||pwd), uname = encrypt_aes(uname), email = encrypt_aes(email) where nzp_user >= 3 and nzp_user  < 100;


insert into userp (nzp_usp, nzp_role, nzp_user, sign)
select 0,r.nzp_role, u.nzp_user, '' from users u, s_roles r 
where  u.nzp_user >= 3  and u.nzp_user < 100 and r.nzp_role in (10,11,12,13,14);

update userp set sign = encrypt_aes(nzp_user||nzp_role||'-'||nzp_usp||'userp') where nzp_user >= 3 and nzp_user < 100;
