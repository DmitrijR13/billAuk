database websahbi;
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
insert into config(nzp_role) values (947); -- Картотека-Саха

insert into config(nzp_role) values (11); -- Аналитика
insert into config(nzp_role) values (931); -- Аналитика-Специфика-Зеленодольск-РТ
insert into config(nzp_role) values (905); -- Аналитика-Изменение

insert into config(nzp_role) values (12); -- Администратор
insert into config(nzp_role) values (932); -- Администратор-Специфика-Самара

insert into config(nzp_role) values (14); -- Паспортистка
insert into config(nzp_role) values (925); -- Паспортистка-Специфика-Зеленодольск

insert into config(nzp_role) values (18); -- Скрывать персональные данные

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));