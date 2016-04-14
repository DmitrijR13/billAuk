database webgub;
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
insert into config(nzp_role) values (10);
insert into config(nzp_role) values (11);
insert into config(nzp_role) values (12);
insert into config(nzp_role) values (13);
insert into config(nzp_role) values (14);
insert into config(nzp_role) values (15);
insert into config(nzp_role) values (18);
insert into config(nzp_role) values (19);
insert into config(nzp_role) values (20);
insert into config(nzp_role) values (901);
insert into config(nzp_role) values (903);
insert into config(nzp_role) values (904);
insert into config(nzp_role) values (905);
insert into config(nzp_role) values (915);
insert into config(nzp_role) values (919);
insert into config(nzp_role) values (920);
insert into config(nzp_role) values (921);
insert into config(nzp_role) values (927);
insert into config(nzp_role) values (929);
insert into config(nzp_role) values (930);
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
insert into config(nzp_role) values (949);
insert into config(nzp_role) values (950);
insert into config(nzp_role) values (951);
insert into config(nzp_role) values (953); -- роль Картотека-Специфика-Губкин
insert into config(nzp_role) values (954); -- роль Паспортистка-Специфика-Губкин

update config set sign = encrypt_aes(nzp_role||'-'||nzp_config||'config');

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,330,encrypt_aes(1));

