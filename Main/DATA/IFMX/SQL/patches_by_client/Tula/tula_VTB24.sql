-- DROP TABLE tula_vtb24_d;

CREATE TABLE "are".tula_vtb24_d(
   vtb24_down_id SERIAL NOT NULL,
   message_date DATE, 
   total_amount DECIMAL(14,2),
   commission DECIMAL(14,2),
   count_oper INTEGER,
   download_date DATETIME YEAR to SECOND,
   user_d INTEGER,
   file_name CHAR(100),
   file_id INTEGER,
   file_type CHAR(50),
   sender CHAR(150),
   receiver CHAR(150),
   nzp_status integer,
   nzp_exc integer,
   nzp_bank integer );                                                    

CREATE UNIQUE INDEX "are".ix_tula_vtb24_d_1 ON "are".tula_vtb24_d(vtb24_down_id);





--DROP TABLE tula_vtb24;

CREATE TABLE "are".tula_vtb24(
   vtb24_id SERIAL NOT NULL,
   vtb24_down_id INTEGER NOT NULL,
   operation_uni CHAR(100),
   number INTEGER,
   date_operation DATETIME YEAR to SECOND,
   account DECIMAL(13,0),
   amount DECIMAL(14,2),
   commission DECIMAL(14,2),
   sum_money DECIMAL(14,2),
   nzp_kvar INTEGER);
   
CREATE UNIQUE INDEX "are".ix_tula_vtb24_1 ON "are".tula_vtb24(vtb24_id);


--DROP TABLE "are".tula_vtb24_log;

CREATE TABLE "are".tula_vtb24_log(
   nzp_log SERIAL NOT NULL,
   vtb24_down_id INTEGER NOT NULL,   
   errors CHAR(250),
   date_log DATETIME YEAR to SECOND);     

CREATE UNIQUE INDEX "are".ix_tula_vtb24_log ON "are".tula_vtb24_log(nzp_log);


