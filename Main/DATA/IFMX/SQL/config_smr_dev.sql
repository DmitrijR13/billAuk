database websmr;
set encryption password "IfmxPwd2";

CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
  
	drop table config;

END PROCEDURE;
EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;


CREATE PROCEDURE tshu_drp() on exception return; end exception with resume 
  
	create table "webdb".config
	(
		nzp_config serial not null,
		nzp_role int,
		sign char(100)
	);
	CREATE INDEX "webdb".ix_config_1 ON "webdb".config(nzp_role);

END PROCEDURE;
EXECUTE PROCEDURE tshu_drp(); DROP PROCEDURE tshu_drp;

delete from config;

insert into config(nzp_role) values (10); -- Картотека
insert into config(nzp_role) values (11); -- Аналитика
insert into config(nzp_role) values (12); -- Администратор
insert into config(nzp_role) values (13); -- Приборы учета
insert into config(nzp_role) values (14); -- Паспортистка
insert into config(nzp_role) values (15); -- Финансы
insert into config(nzp_role) values (18); -- Скрывать персональные данные
insert into config(nzp_role) values (20); -- Касса
insert into config(nzp_role) values (26); -- Настройка системы
insert into config(nzp_role) values (30); -- Отчеты
insert into config(nzp_role) values (901); -- Картотека-Изменение
insert into config(nzp_role) values (903); -- Приборы учета-Изменение
insert into config(nzp_role) values (904); -- Паспортистка-Изменение
insert into config(nzp_role) values (905); -- Аналитика-Изменение
insert into config(nzp_role) values (915); -- Финансы-Изменение
insert into config(nzp_role) values (920); -- Касса-Изменение
insert into config(nzp_role) values (921); -- Расчет начислений
insert into config(nzp_role) values (922); -- Картотека-Специфика-Самара
insert into config(nzp_role) values (923); -- Паспортистка-Специфика-Самара
insert into config(nzp_role) values (927); -- Картотека-Изменение-Специфика-Самара
insert into config(nzp_role) values (929); -- Паспортистка-Изменение-Специфика-Самара
insert into config(nzp_role) values (930); -- Аналитика-Специфика-Самара
insert into config(nzp_role) values (932); -- Администратор-Специфика-Самара
insert into config(nzp_role) values (933); -- Справочники в Картотеке (редактирование)
insert into config(nzp_role) values (939); -- Приборы учета-Редактирование-Самара
insert into config(nzp_role) values (940); -- Приборы учета (Редактирование)-Списочный ввод показаний
insert into config(nzp_role) values (941); -- Приборы учета-Списочный ввод показаний
insert into config(nzp_role) values (942); -- Финансы-Справочники (Редактирование)
insert into config(nzp_role) values (978); -- Выгрузка в банк-клиент
insert into config(nzp_role) values (979); -- Настройка форматов банк-клиент
insert into config(nzp_role) values (980); -- управление уникальными ключами
insert into config(nzp_role) values (987); -- Сальдо по перечислениям по домам
insert into config(nzp_role) values (988); -- Управление очередями
insert into config(nzp_role) values (993); -- Справочник параметров
insert into config(nzp_role) values (994); -- Смена УК

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));


--Добавление координат на карту
delete from map_points where nzp_mo in (select nzp_mo from map_objects where tip in (-1,-2));
delete from map_objects where tip in (-1,-2);
-- ключ для stcline.ru
insert into map_objects (tip, note) values (-1, 'ANxfQU0BAAAA-5JTKQIAZqDm6jpY7AMd5oxJ_0cDue0pX9EAAAAAAAAAAACfHJffTds2p14Rtjd4WeeUBjTFGA==');
insert into map_objects (tip, note) values (-2, 'г. Самара');
insert into map_points (nzp_mo, x, y) select nzp_mo, 50.196333, 53.244172 from map_objects where tip = -2;


