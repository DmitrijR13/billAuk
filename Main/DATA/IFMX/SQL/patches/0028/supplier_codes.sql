CREATE TABLE supplier_codes(
   nzp_sc SERIAL NOT NULL,
   nzp_kvar INTEGER,
   nzp_supp INTEGER,
   kod_geu INTEGER,
   pkod10 INTEGER default 0,
   pkod_supp DECIMAL(13) default 0.0000000000000000 NOT NULL);