-- в центральной data (fastr_data)

CREATE TABLE saldo_date_area(
   id SERIAL NOT NULL,
   nzp_area INTEGER NOT NULL,
   month_ INTEGER NOT NULL,
   year_ INTEGER NOT NULL,
   is_current SMALLINT,
   prev_month INTEGER,
   prev_year INTEGER,
   changed_by INTEGER,
   changed_on TIMESTAMP);