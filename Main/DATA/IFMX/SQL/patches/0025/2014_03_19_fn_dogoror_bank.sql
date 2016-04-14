--central data

CREATE TABLE fn_dogovor_bank(
   nzp_fd_bank SERIAL NOT NULL,
   nzp_fd INTEGER,
   nzp_bank INTEGER,
   dat_when DATE,
   nzp_user INTEGER);

CREATE INDEX ix_fdbank_2 ON fn_dogovor_bank(nzp_fd);

CREATE INDEX ix_fdbank_3 ON fn_dogovor_bank(nzp_bank);

CREATE UNIQUE INDEX ix_fdbank_1 ON fn_dogovor_bank(nzp_fd_bank);