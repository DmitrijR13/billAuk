
--database local_data;
--database center_data;

CREATE TABLE kredit
(
  nzp_kredit serial NOT NULL,
  nzp_kvar integer NOT NULL,
  nzp_serv integer NOT NULL,
  dat_month date NOT NULL,
  dat_s date NOT NULL,
  dat_po date NOT NULL,
  valid integer NOT NULL,
  sum_dolg numeric(14,2) DEFAULT 0.00,
  perc numeric(5,2) DEFAULT 0.00,
  dog_num character(20),
  dog_dat date,
  sum_real_p numeric(14,2)
);

--database local_charge_XX; создать 12 таблиц: с kredit_01 ... до kredit_12 

CREATE TABLE kredit_01
(
  nzp_kredx serial NOT NULL,
  nzp_kvar integer NOT NULL,
  nzp_serv integer NOT NULL,
  nzp_kredit integer NOT NULL,
  sum_indolg numeric(14,2) DEFAULT 0.00,
  sum_dolg numeric(14,2) DEFAULT 0.00,
  sum_odna12 numeric(14,2) DEFAULT 0.00,
  sum_perc numeric(14,2) DEFAULT 0.00,
  sum_charge numeric(14,2) DEFAULT 0.00,
  sum_outdolg numeric(14,2) DEFAULT 0.00,
  sum_money numeric(14,2)
);

CREATE UNIQUE INDEX ixkrx1__01 ON astr01_charge_14.kredit_01 USING btree (nzp_kredx);

CREATE        INDEX ixkrx2__01 ON astr01_charge_14.kredit_01 USING btree (nzp_kvar, nzp_serv);

CREATE        INDEX ixkrx3__01 ON astr01_charge_14.kredit_01 USING btree (nzp_kredit);

