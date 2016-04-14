-- CENTRAL_KERNEL
drop table req_status_links;
drop table s_req_status;

-- статусы заявки на финансирование
create table "are".s_req_status
(
nzp_status integer not null,
status char(30)
);

create unique index ix_s_req_status_1 on s_req_status(nzp_status);
ALTER TABLE s_req_status ADD CONSTRAINT PRIMARY KEY (nzp_status) CONSTRAINT "are".pk_s_req_status;

insert into "are".s_req_status (nzp_status, status) values (1, 'Формируется');
insert into "are".s_req_status (nzp_status, status) values (2, 'Внесен');
insert into "are".s_req_status (nzp_status, status) values (3, 'Частично перечислен');
insert into "are".s_req_status (nzp_status, status) values (4, 'Перечислен');
insert into "are".s_req_status (nzp_status, status) values (5, 'Удален');


CREATE TABLE "are".req_status_links(
   nzp_link SERIAL NOT NULL,
   nzp_status_from INTEGER NOT NULL,
   nzp_status_to INTEGER NOT NULL,
   is_active INTEGER NOT NULL)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_req_status_links_1 ON "are".req_status_links(nzp_link);
CREATE UNIQUE INDEX "are".ix_req_status_links_2 ON "are".req_status_links(nzp_status_from, nzp_status_to);
CREATE INDEX "are".ix_req_status_links_3 ON "are".req_status_links(nzp_status_from);
CREATE INDEX "are".ix_req_status_links_4 ON "are".req_status_links(nzp_status_to);

insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (1, 2, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (1, 5, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (2, 3, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (2, 4, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (2, 5, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (3, 4, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (3, 2, 1);
insert into "are".req_status_links (nzp_status_from, nzp_status_to, is_active) values (4, 2, 1);


-- CENTRAL_FIN_YY
drop table subs_saldo;
drop table subs_req_details;
drop table subs_req;
drop table subs_order_payer;
drop table subs_order;

-- приказ о перечислении средств подрядчику
CREATE TABLE "are".subs_order(
   nzp_order SERIAL NOT NULL,
   num_doc CHAR(20),
   dat_doc DATE,
   sum_order DECIMAL(14,2) default 0.00,
   comment char(250),
   date_month DATE, -- дата учета
   created_by INTEGER NOT NULL,
   created_on datetime year to second NOT NULL,
   changed_by INTEGER,
   changed_on datetime year to second
) LOCK MODE ROW;
CREATE UNIQUE INDEX "are".ix_subs_order_1 ON "are".subs_order(nzp_order);
ALTER TABLE subs_order ADD CONSTRAINT PRIMARY KEY (nzp_order) CONSTRAINT "are".pk_subs_order;


-- детализация перечисления по домам
CREATE TABLE "are".subs_order_payer(
   nzp_order_payer SERIAL NOT NULL,
   nzp_order INTEGER NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_contract INTEGER NOT NULL
) LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_subs_order_payer_1 ON "are".subs_order_payer(nzp_order_payer);
create unique index "are".ix_subs_order_payer_2 on subs_order_payer(nzp_order, nzp_payer, nzp_contract);
CREATE INDEX "are".ix_subs_order_payer_3 ON "are".subs_order_payer(nzp_order);
CREATE INDEX "are".ix_subs_order_payer_4 ON "are".subs_order_payer(nzp_payer);
CREATE INDEX "are".ix_subs_order_payer_5 ON "are".subs_order_payer(nzp_contract);
ALTER TABLE subs_order_payer ADD CONSTRAINT (FOREIGN KEY (nzp_order) REFERENCES subs_order CONSTRAINT "are".fk_subs_order_payer_1);


create table "are".subs_req
(nzp_req serial not null,
num_request integer not null,
date_request date not null,
date_month date not null,
sum_request decimal(14,2) default 0,
comment char(250),
nzp_status INTEGER NOT NULL,
created_by INTEGER NOT NULL,
created_on datetime year to second NOT NULL,
changed_by INTEGER,
changed_on datetime year to second
) lock mode row;

create unique index "are".ix_subs_req_1 on subs_req(nzp_req);
ALTER TABLE subs_req ADD CONSTRAINT PRIMARY KEY (nzp_req) CONSTRAINT "are".pk_subs_req;
 

create table "are".subs_req_details
(nzp_req_det serial not null,
nzp_req integer not null,
nzp_town integer not null,
nzp_serv integer not null,
nzp_payer integer not null,
nzp_order integer,
sum_pere decimal(14,2) default 0.00,
sum_plan decimal(14,2) default 0.00,
sum_charge decimal(14,2) default 0.00,
sum_izm decimal(14,2) default 0.00,
sum_request decimal(14,2) default 0.00,
changed_by INTEGER,
changed_on datetime year to second
) lock mode row;

create unique index "are".ix_subs_req_details_1 on subs_req_details(nzp_req_det);
create index "are".ix_subs_req_details_2 on subs_req_details(nzp_req);
create index "are".ix_subs_req_details_3 on subs_req_details(nzp_town);
create index "are".ix_subs_req_details_4 on subs_req_details(nzp_serv);
create index "are".ix_subs_req_details_5 on subs_req_details(nzp_payer);
create index "are".ix_subs_req_details_6 on subs_req_details(nzp_order);
ALTER TABLE subs_req_details ADD CONSTRAINT (FOREIGN KEY (nzp_req) REFERENCES subs_req CONSTRAINT "are".fk_subs_req_details_1);
ALTER TABLE subs_req_details ADD CONSTRAINT (FOREIGN KEY (nzp_order) REFERENCES subs_order CONSTRAINT "are".fk_subs_req_details_2);


create table "are".subs_saldo
(nzp_subs serial not null,
nzp_town INTEGER NOT NULL,
nzp_serv INTEGER NOT NULL,
nzp_payer INTEGER NOT NULL,
date_month date not null,
sum_insaldo decimal(14,2) default 0.00,
sum_charge decimal(14,2) default 0.00,
sum_request decimal(14,2) default 0.00,
sum_order decimal(14,2) default 0.00,
sum_mismatch decimal(14,2) default 0.00,
sum_outsaldo decimal(14,2) default 0.00
) lock mode row;

create unique index "are".ix_subs_saldo_1 on subs_saldo(nzp_subs);
create unique index "are".ix_subs_saldo_2 on subs_saldo(nzp_town, nzp_payer, nzp_serv, date_month);
create index "are".ix_subs_saldo_3 on subs_saldo(nzp_town);
create index "are".ix_subs_saldo_4 on subs_saldo(nzp_serv);
create index "are".ix_subs_saldo_5 on subs_saldo(nzp_payer);


-- CENTRAL_DATA
drop table subs_contract;
drop table subs_contr_types;

-- счета подрядчиков
CREATE TABLE "are".fn_bank(
   nzp_fb SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   num_count INTEGER NOT NULL,
   bank_name CHAR(100),
   rcount CHAR(30),
   kcount CHAR(30),
   bik CHAR(30),
   npunkt CHAR(60),
   kpp CHAR(20),
   nzp_user INTEGER,
   dat_when DATE
) LOCK MODE ROW;


-- типы соглашений
CREATE TABLE "are".subs_contr_types(
   nzp_type serial not null,
   full_name char(200) not null,
   short_name char(50) not null
) LOCK MODE ROW;
CREATE UNIQUE INDEX "are".ix_subs_contr_types_1 ON "are".subs_contr_types(nzp_type);
CREATE UNIQUE INDEX "are".ix_subs_contr_types_2 ON "are".subs_contr_types(full_name);
CREATE UNIQUE INDEX "are".ix_subs_contr_types_3 ON "are".subs_contr_types(short_name);
ALTER TABLE subs_contr_types ADD CONSTRAINT PRIMARY KEY (nzp_type) CONSTRAINT "are".pk_subs_contr_types;

insert into "are".subs_contr_types (nzp_type, full_name, short_name) values (1, 'Основное соглашение', 'Осн. согл.');
insert into "are".subs_contr_types (nzp_type, full_name, short_name) values (2, 'Дополнительное соглашение', 'Доп. согл.');


-- соглашения о предоставлении субсидий
CREATE TABLE "are".subs_contract(
   nzp_contract SERIAL NOT NULL,
   nzp_payer INTEGER NOT NULL,
   nzp_town INTEGER,
   num_doc CHAR(20),
   date_doc DATE,
   date_begin DATE NOT NULL,
   date_end DATE NOT NULL,
   nzp_fb INTEGER NOT NULL, -- счет в банке
   nzp_type INTEGER NOT NULL, --тип соглашения
   comment CHAR(250),
   created_by INTEGER NOT NULL,
   created_on datetime year to second NOT NULL,
   changed_by INTEGER,
   changed_on datetime year to second
) LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_subs_contract_1 ON "are".subs_contract(nzp_contract);
CREATE INDEX "are".ix_subs_contract_2 ON "are".subs_contract(nzp_payer);
CREATE INDEX "are".ix_subs_contract_3 ON "are".subs_contract(nzp_town);
CREATE INDEX "are".ix_subs_contract_4 ON "are".subs_contract(nzp_fb);
CREATE INDEX "are".ix_subs_contract_5 ON "are".subs_contract(nzp_type);
ALTER TABLE subs_contract ADD CONSTRAINT PRIMARY KEY (nzp_contract) CONSTRAINT "are".pk_subs_contract;
ALTER TABLE subs_contract ADD CONSTRAINT (FOREIGN KEY (nzp_type) REFERENCES subs_contr_types CONSTRAINT "are".fk_subs_contract_1);
