--CENTRAL_fin_YY;

-- create unique index ix_pack_1 on pack(nzp_pack);
-- alter table pack_ls drop constraint fk_pack_ls_1;
-- alter table pack drop constraint pk_pack;
alter table pack add constraint PRIMARY KEY (nzp_pack) CONSTRAINT pk_pack;
ALTER TABLE pack_ls add CONSTRAINT FOREIGN KEY (nzp_pack) REFERENCES pack(nzp_pack) CONSTRAINT fk_pack_ls_1;

-- create unique index ix_pack_ls_1 on pack_ls(nzp_pack_ls); --try
-- alter table gil_sums drop constraint fk_gil_sums_1;
-- alter table pack_ls drop constraint pk_pack_ls;
alter table pack_ls add constraint PRIMARY KEY (nzp_pack_ls) CONSTRAINT pk_pack_ls;
ALTER TABLE gil_sums aDD CONSTRAINT FOREIGN KEY (nzp_pack_ls) REFERENCES pack_ls(nzp_pack_ls) CONSTRAINT fk_gil_sums_1;
