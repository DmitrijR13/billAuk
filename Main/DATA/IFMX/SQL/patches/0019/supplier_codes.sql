--все локальные data
create table supplier_codes
(
      nzp_sc SERIAL NOT NULL,
      nzp_kvar integer,
      nzp_supp integer,
      kod_geu integer,
      pkod10 INTEGER default 0,
      pkod_supp DECIMAL(13) default 0.0000000000000000 NOT NULL                     
);
