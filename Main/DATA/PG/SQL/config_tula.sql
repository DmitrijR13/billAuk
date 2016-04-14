set search_path to public;

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
insert into config(nzp_role) values (10);
insert into config(nzp_role) values (11);
insert into config(nzp_role) values (12);
insert into config(nzp_role) values (13);
insert into config(nzp_role) values (14);
insert into config(nzp_role) values (15);
insert into config(nzp_role) values (18);
insert into config(nzp_role) values (19);
insert into config(nzp_role) values (20);
insert into config(nzp_role) values (26);
insert into config(nzp_role) values (27);
insert into config(nzp_role) values (30);
insert into config(nzp_role) values (901);
insert into config(nzp_role) values (903);
insert into config(nzp_role) values (904);
insert into config(nzp_role) values (905);
insert into config(nzp_role) values (915);
insert into config(nzp_role) values (919);
insert into config(nzp_role) values (920);
insert into config(nzp_role) values (921);
insert into config(nzp_role) values (929);
insert into config(nzp_role) values (932);
insert into config(nzp_role) values (933);
insert into config(nzp_role) values (934);
insert into config(nzp_role) values (935);
insert into config(nzp_role) values (936);
insert into config(nzp_role) values (937);
insert into config(nzp_role) values (939);
insert into config(nzp_role) values (940);
insert into config(nzp_role) values (941);
insert into config(nzp_role) values (942);
insert into config(nzp_role) values (950);
insert into config(nzp_role) values (951);
insert into config(nzp_role) values (954); -- роль Паспортистка-Специфика-Губкин
insert into config(nzp_role) values (958); -- роль Картотека-Специфика-Тула
insert into config(nzp_role) values (960);
insert into config(nzp_role) values (965); -- роль Картотека-Изменения-Специфика-Тула
insert into config(nzp_role) values (975); -- Генерация индивидуальных приборов учета
insert into config(nzp_role) values (976); -- взаим с СЗ
insert into config(nzp_role) values (978); -- Выгрузка банк - клиент
insert into config(nzp_role) values (979); -- Настройка формата банк - клиент
insert into config(nzp_role) values (980); -- Настройка уникальных кодов
insert into config(nzp_role) values (981); -- взаим с внеш системами
insert into config(nzp_role) values (984); -- быстрый ввод ПУ
insert into config(nzp_role) values (985); -- Загрузка оплат от ВТБ24
insert into config(nzp_role) values (986); -- Управление связанными услугами
insert into config(nzp_role) values (990); -- Закрытие месяца(подготовка данный для печати счетов)
insert into config(nzp_role) values (991); -- Генерация платежных кодов
insert into config(nzp_role) values (993); -- Справочник параметров
insert into config(nzp_role) values (998); -- Взаимодействие с Банком
insert into config(nzp_role) values (999); -- Сальдо по перечислениям по домам по договорам
insert into config(nzp_role) values (800); -- Проценты удержания по договорам
insert into config(nzp_role) values (801); -- Редактирование процентов удержания по договорам
insert into config(nzp_role) values (802); -- Просмотр договоров
insert into config(nzp_role) values (803); -- Редактирование договоров
insert into config(nzp_role) values (804); -- Просмотр контрагентов
insert into config(nzp_role) values (805); -- Редактирование контрагентов
insert into config(nzp_role) values (806); -- Редактирование сальдо по перечислениям
insert into config(nzp_role) values (809); -- Просмотр управляющих организаций
insert into config(nzp_role) values (810); -- Редактирование управляющих организаций
insert into config(nzp_role) values (811); -- Просмотр статистики по начислениям
insert into config(nzp_role) values (812); -- Редактирование параметров договоров
insert into config(nzp_role) values (813); -- Изменение адреса лицевых счетов
insert into config(nzp_role) values (814); -- Проверки перед закрытием месяца


update config set sign = nzp_role::varchar||'-'||nzp_config::varchar||'config';

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,1);
