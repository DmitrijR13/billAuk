--в центральной data

CREATE TABLE "are".saldo_date_area(
   id SERIAL NOT NULL,
   nzp_area INTEGER NOT NULL,
   month_ INTEGER NOT NULL,
   year_ INTEGER NOT NULL,
   is_current SMALLINT,
   prev_month INTEGER,
   prev_year INTEGER,
   changed_by INTEGER,
   changed_on DATETIME YEAR to SECOND)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE ROW;