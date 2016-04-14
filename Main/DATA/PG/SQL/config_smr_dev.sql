CREATE table if not exists config
(
  nzp_config serial NOT NULL,
  nzp_role integer,
  sign character(100)
)
WITH OIDS;

ALTER TABLE config OWNER TO postgres;
GRANT ALL ON TABLE config TO postgres;
GRANT SELECT, UPDATE, INSERT, DELETE ON TABLE config TO public;

drop index if exists ix_config_1;
CREATE INDEX ix_config_1 ON config USING btree (nzp_role);

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
insert into config(nzp_role) values (960); -- Загрузка наследуемой информации
insert into config(nzp_role) values (980); -- Управление первичными ключами
insert into config(nzp_role) values (988); -- Управление очередями задач

update config set sign = nzp_role::varchar||'-'||nzp_config::varchar||'config';

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,1);
