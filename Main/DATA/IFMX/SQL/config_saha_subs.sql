database websaha;
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

insert into config(nzp_role) values (12); -- Администратор
insert into config(nzp_role) values (22); -- Дотации ЮЛ
insert into config(nzp_role) values (23); -- Расчет дотаций ЮЛ
insert into config(nzp_role) values (24); -- Субсидии ФЛ
insert into config(nzp_role) values (921); -- Расчет начислений
insert into config(nzp_role) values (924); -- Картотека-Специфика-Зеленодольск
insert into config(nzp_role) values (928); -- Картотека-Изменение-Специфика-Зеленодольск
insert into config(nzp_role) values (932); -- Администратор-Специфика-Самара
insert into config(nzp_role) values (933); -- Справочники в Картотеке (редактирование)
insert into config(nzp_role) values (942); -- Финансы-Справочники (Редактирование)
insert into config(nzp_role) values (944); -- Финансирование дотаций-Редактирование
insert into config(nzp_role) values (945); -- Расчет дотаций-Редактирование

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,encrypt_aes(1));