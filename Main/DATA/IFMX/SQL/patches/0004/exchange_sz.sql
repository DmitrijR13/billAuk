--central_data
--DROP TABLE "are".tula_ex_sz;

CREATE TABLE "are".tula_ex_sz(
   nzp_ex_sz SERIAL NOT NULL,
   file_name CHAR(50),
   dat_upload DATETIME YEAR to SECOND,
   nzp_user INTEGER )
LOCK MODE ROW;

CREATE UNIQUE INDEX "are".ix_tula_ex_sz_1 ON "are".tula_ex_sz(nzp_ex_sz);


--central_data
--DROP TABLE "are".tula_ex_sz_file;

CREATE TABLE "are".tula_ex_sz_file(
   id SERIAL NOT NULL,
   famil CHAR(50),
   imja CHAR(50),
   otch CHAR(50),
   drog DATE,
   strahnm CHAR(14),
   nasp CHAR(50),
   nylic CHAR(50),
   ndom CHAR(7),
   nkorp CHAR(3),
   nkw CHAR(15),
   nkomn CHAR(15),
   kolk INTEGER,
   lchet CHAR(24),
   vidgf CHAR(25),
   privat CHAR(1),
   opl DECIMAL(8,2),
   otpl DECIMAL(8,2),
   otplj DECIMAL(8,2),
   kolzr INTEGER,
   kolpr INTEGER,
   prz INTEGER,
   prn INTEGER,
   prk INTEGER,
   gku1 CHAR(100),
   tarif1 DECIMAL(15,5),
   sum1 DECIMAL(14,4),
   fakt1 DECIMAL(14,4),
   org1 CHAR(30),
   vidtar1 INTEGER,
   koef1 DECIMAL(12,2),
   lchet1 CHAR(24),
   sumz1 DECIMAL(14,4),
   klmz1 INTEGER,
   ozs1 INTEGER,
   sumozs1 DECIMAL(14,4),
   gku2 CHAR(100),
   tarif2 DECIMAL(15,5),
   sum2 DECIMAL(14,4),
   fakt2 DECIMAL(14,4),
   org2 CHAR(30),
   vidtar2 INTEGER,
   koef2 DECIMAL(12,2),
   lchet2 CHAR(24),
   sumz2 DECIMAL(14,4),
   klmz2 INTEGER,
   ozs2 INTEGER,
   sumozs2 DECIMAL(14,4),
   gku3 CHAR(100),
   tarif3 DECIMAL(15,5),
   sum3 DECIMAL(14,4),
   fakt3 DECIMAL(14,4),
   org3 CHAR(30),
   vidtar3 INTEGER,
   koef3 DECIMAL(12,2),
   lchet3 CHAR(24),
   sumz3 DECIMAL(14,4),
   klmz3 INTEGER,
   ozs3 INTEGER,
   sumozs3 DECIMAL(14,4),
   gku4 CHAR(100),
   tarif4 DECIMAL(15,5),
   sum4 DECIMAL(14,4),
   fakt4 DECIMAL(14,4),
   org4 CHAR(30),
   vidtar4 INTEGER,
   koef4 DECIMAL(12,2),
   lchet4 CHAR(24),
   sumz4 DECIMAL(14,4),
   klmz4 INTEGER,
   ozs4 INTEGER,
   sumozs4 DECIMAL(14,4),
   gku5 CHAR(100),
   tarif5 DECIMAL(15,5),
   sum5 DECIMAL(14,4),
   fakt5 DECIMAL(14,4),
   org5 CHAR(30),
   vidtar5 INTEGER,
   koef5 DECIMAL(12,2),
   lchet5 CHAR(24),
   sumz5 DECIMAL(14,4),
   klmz5 INTEGER,
   ozs5 INTEGER,
   sumozs5 DECIMAL(14,4),
   gku6 CHAR(100),
   tarif6 DECIMAL(15,5),
   sum6 DECIMAL(14,4),
   fakt6 DECIMAL(14,4),
   org6 CHAR(30),
   vidtar6 INTEGER,
   koef6 DECIMAL(12,2),
   lchet6 CHAR(24),
   sumz6 DECIMAL(14,4),
   klmz6 INTEGER,
   ozs6 INTEGER,
   sumozs6 DECIMAL(14,4),
   gku7 CHAR(100),
   tarif7 DECIMAL(15,5),
   sum7 DECIMAL(14,4),
   fakt7 DECIMAL(14,4),
   org7 CHAR(30),
   vidtar7 INTEGER,
   koef7 DECIMAL(12,2),
   lchet7 CHAR(24),
   sumz7 DECIMAL(14,4),
   klmz7 INTEGER,
   ozs7 INTEGER,
   sumozs7 DECIMAL(14,4),
   gku8 CHAR(100),
   tarif8 DECIMAL(15,5),
   sum8 DECIMAL(14,4),
   fakt8 DECIMAL(14,4),
   org8 CHAR(30),
   vidtar8 INTEGER,
   koef8 DECIMAL(12,2),
   lchet8 CHAR(24),
   sumz8 DECIMAL(14,4),
   klmz8 INTEGER,
   ozs8 INTEGER,
   sumozs8 DECIMAL(14,4),
   gku9 CHAR(100),
   tarif9 DECIMAL(15,5),
   sum9 DECIMAL(14,4),
   fakt9 DECIMAL(14,4),
   org9 CHAR(30),
   vidtar9 INTEGER,
   koef9 DECIMAL(12,2),
   lchet9 CHAR(24),
   sumz9 DECIMAL(14,4),
   klmz9 INTEGER,
   ozs9 INTEGER,
   sumozs9 DECIMAL(14,4),
   gku10 CHAR(100),
   tarif10 DECIMAL(15,5),
   sum10 DECIMAL(14,4),
   fakt10 DECIMAL(14,4),
   org10 CHAR(30),
   vidtar10 INTEGER,
   koef10 DECIMAL(12,2),
   lchet10 CHAR(24),
   sumz10 DECIMAL(14,4),
   klmz10 INTEGER,
   ozs10 INTEGER,
   sumozs10 DECIMAL(14,4),
   gku11 CHAR(100),
   tarif11 DECIMAL(15,5),
   sum11 DECIMAL(14,4),
   fakt11 DECIMAL(14,4),
   org11 CHAR(30),
   vidtar11 INTEGER,
   koef11 DECIMAL(12,2),
   lchet11 CHAR(24),
   sumz11 DECIMAL(14,4),
   klmz11 INTEGER,
   ozs11 INTEGER,
   sumozs11 DECIMAL(14,4),
   gku12 CHAR(100),
   tarif12 DECIMAL(15,5),
   sum12 DECIMAL(14,4),
   fakt12 DECIMAL(14,4),
   org12 CHAR(30),
   vidtar12 INTEGER,
   koef12 DECIMAL(12,2),
   lchet12 CHAR(24),
   sumz12 DECIMAL(14,4),
   klmz12 INTEGER,
   ozs12 INTEGER,
   sumozs12 DECIMAL(14,4),
   gku13 CHAR(100),
   tarif13 DECIMAL(15,5),
   sum13 DECIMAL(14,4),
   fakt13 DECIMAL(14,4),
   org13 CHAR(30),
   vidtar13 INTEGER,
   koef13 DECIMAL(12,2),
   lchet13 CHAR(24),
   sumz13 DECIMAL(14,4),
   klmz13 INTEGER,
   ozs13 INTEGER,
   sumozs13 DECIMAL(14,4),
   gku14 CHAR(100),
   tarif14 DECIMAL(15,5),
   sum14 DECIMAL(14,4),
   fakt14 DECIMAL(14,4),
   org14 CHAR(30),
   vidtar14 INTEGER,
   koef14 DECIMAL(12,2),
   lchet14 CHAR(24),
   sumz14 DECIMAL(14,4),
   klmz14 INTEGER,
   ozs14 INTEGER,
   sumozs14 DECIMAL(14,4),
   gku15 CHAR(100),
   tarif15 DECIMAL(15,5),
   sum15 DECIMAL(14,4),
   fakt15 DECIMAL(14,4),
   org15 CHAR(30),
   vidtar15 INTEGER,
   koef15 DECIMAL(12,2),
   lchet15 CHAR(24),
   sumz15 DECIMAL(14,4),
   klmz15 INTEGER,
   ozs15 INTEGER,
   sumozs15 DECIMAL(14,4),
   nzp_ex_sz integer,
   nzp_kvar integer)
   LOCK MODE ROW;
   
    CREATE UNIQUE INDEX "are".ix_tula_ex_sz_file_1 ON "are".tula_ex_sz_file(id);