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
insert into config(nzp_role) values (19); -- Учет претензий
insert into config(nzp_role) values (20); -- Касса
insert into config(nzp_role) values (901); -- Картотека-Изменение
insert into config(nzp_role) values (903); -- Приборы учета-Изменение
insert into config(nzp_role) values (904); -- Паспортистка-Изменение
insert into config(nzp_role) values (905); -- Аналитика-Изменение
insert into config(nzp_role) values (915); -- Финансы-Изменение
insert into config(nzp_role) values (919); -- Администратор УПГ
insert into config(nzp_role) values (920); -- Касса-Изменение
insert into config(nzp_role) values (921); -- Расчет начислений
insert into config(nzp_role) values (922); -- Картотека-Специфика-Самара
insert into config(nzp_role) values (923); -- Паспортистка-Специфика-Самара
insert into config(nzp_role) values (927); -- Картотека-Изменение-Специфика-Самара
insert into config(nzp_role) values (929); -- Паспортистка-Изменение-Специфика-Самара
insert into config(nzp_role) values (930); -- Аналитика-Специфика-Самара
insert into config(nzp_role) values (932); -- Администратор-Специфика-Самара
insert into config(nzp_role) values (933); -- Справочники в Картотеке (редактирование)
insert into config(nzp_role) values (934); -- Оператор УПГ
insert into config(nzp_role) values (935); -- Диспетчер УПГ
insert into config(nzp_role) values (936); -- Подрядчик УПГ
insert into config(nzp_role) values (937); -- УК УПГ
insert into config(nzp_role) values (939); -- Приборы учета-Редактирование-Самара
insert into config(nzp_role) values (940); -- Приборы учета (Редактирование)-Списочный ввод показаний
insert into config(nzp_role) values (941); -- Приборы учета-Списочный ввод показаний
insert into config(nzp_role) values (942); -- Финансы-Справочники (Редактирование)
insert into config(nzp_role) values (946); -- Картотека (Распределение недопоставок УПГ)
insert into config(nzp_role) values (949); -- Рассылка сообщений (УПГ)
insert into config(nzp_role) values (950); -- Выгрузка недопоставок (УПГ)
insert into config(nzp_role) values (951); -- Обновление адресов (УПГ)
insert into config(nzp_role) values (990); -- Закрытие месяца(подготовка данный для печати счетов)

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));