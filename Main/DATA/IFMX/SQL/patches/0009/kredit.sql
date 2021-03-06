-- local data
CREATE TABLE "are".kredit(
   nzp_kredit SERIAL NOT NULL,
   nzp_kvar   INTEGER NOT NULL,
   nzp_serv    INTEGER NOT NULL,
   dat_month   DATE NOT NULL,
   dat_s DATE NOT NULL,
   dat_po DATE NOT NULL,
   valid INTEGER NOT NULL,
   sum_dolg DECIMAL(14,2) default 0.00,
   perc DECIMAL(5,2) default 0.00)
LOCK MODE ROW;