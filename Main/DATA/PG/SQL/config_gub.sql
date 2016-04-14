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
insert into config(nzp_role) values (950);
insert into config(nzp_role) values (951);
insert into config(nzp_role) values (953); -- роль Картотека-Специфика-Губкин
insert into config(nzp_role) values (954); -- роль Паспортистка-Специфика-Губкин
insert into config(nzp_role) values (959);
insert into config(nzp_role) values (960);

update config set sign = nzp_role::varchar||'-'||nzp_config::varchar||'config';

--признак наличия установленного Excel : 1 - установлен, 0 - не установлен
delete from  sysprtdata where  num_prtd in (33,330);
insert into sysprtdata values (0,33,1);
