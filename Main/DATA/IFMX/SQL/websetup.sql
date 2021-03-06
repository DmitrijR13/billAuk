--Добавление таблицы запросов на восстановление паролей
drop table webdb.s_setups;
 
create table webdb.s_setups
( nzp_setup serial not null,
    nzp_param integer,
    param_name char(250),
    param_type char(50),
    value_ char(250),
    nzp_user integer,
    dat_when datetime year to second 
);

create unique index webdb.ix_s_setups_1 on webdb.s_setups(nzp_setup);
create unique index webdb.ix_s_setups_2 on webdb.s_setups(nzp_param);
create unique index webdb.ix_s_setups_3 on webdb.s_setups(param_name);

insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 1, 'SMTP-сервер', 'char', 'mail.stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 2, 'Порт для отправки почты', 'char', '25',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 3, 'Имя пользователя для почтового сервера ', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 4, 'Пароль пользователя для почтового сервера', 'char', 'stcline',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 5, 'Наименование отправителя для исходящих сообщений', 'char', 'STC Line',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 6, 'Email отправителя', 'char', 'portal@stcline.ru',null,null);
insert into s_setups (nzp_setup, nzp_param, param_name, param_type, value_, nzp_user, dat_when) values (0, 7, 'Работать только с центральным банком данных', 'bool', '2', null, null);


set encryption password "IfmxPwd2";

update s_setups set param_name = encrypt_aes(param_name)
, param_type = encrypt_aes(param_type)
, value_ = encrypt_aes(value_);

select decrypt_char(param_name) from s_setups;