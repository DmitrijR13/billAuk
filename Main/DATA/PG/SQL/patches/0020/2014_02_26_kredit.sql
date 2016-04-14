-- local _data
alter table kredit add sum_real_p numeric(14,2) default 0.00;

-- local _charge_14;
-- local _charge_15;
-- перед создание пытаться удалить таблицу
--1
CREATE TABLE kredit_01(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__01 ON kredit_01(nzp_kredx);
CREATE INDEX ixkrx2__01 ON kredit_01(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__01 ON kredit_01(nzp_kredit); 
--2
CREATE TABLE kredit_02(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__02 ON kredit_02(nzp_kredx);
CREATE INDEX ixkrx2__02 ON kredit_02(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__02 ON kredit_02(nzp_kredit); 
--3
CREATE TABLE kredit_03(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__03 ON kredit_03(nzp_kredx);
CREATE INDEX ixkrx2__03 ON kredit_03(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__03 ON kredit_03(nzp_kredit);
--4
CREATE TABLE kredit_04(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__04 ON kredit_04(nzp_kredx);
CREATE INDEX ixkrx2__04 ON kredit_04(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__04 ON kredit_04(nzp_kredit);
--5
CREATE TABLE kredit_05(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__05 ON kredit_05(nzp_kredx);
CREATE INDEX ixkrx2__05 ON kredit_05(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__05 ON kredit_05(nzp_kredit);
--6
CREATE TABLE kredit_06(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__06 ON kredit_06(nzp_kredx);
CREATE INDEX ixkrx2__06 ON kredit_06(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__06 ON kredit_06(nzp_kredit);
-- 7
CREATE TABLE kredit_07(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__07 ON kredit_07(nzp_kredx);
CREATE INDEX ixkrx2__07 ON kredit_07(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__07 ON kredit_07(nzp_kredit);
-- 8
CREATE TABLE kredit_08(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__08 ON kredit_08(nzp_kredx);
CREATE INDEX ixkrx2__08 ON kredit_08(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__08 ON kredit_08(nzp_kredit);
--9
CREATE TABLE kredit_09(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__09 ON kredit_09(nzp_kredx);
CREATE INDEX ixkrx2__09 ON kredit_09(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__09 ON kredit_09(nzp_kredit);
-- 10
CREATE TABLE kredit_10(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__10 ON kredit_10(nzp_kredx);
CREATE INDEX ixkrx2__10 ON kredit_10(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__10 ON kredit_10(nzp_kredit);
--11
CREATE TABLE kredit_11(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__11 ON kredit_11(nzp_kredx);
CREATE INDEX ixkrx2__11 ON kredit_11(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__11 ON kredit_11(nzp_kredit);
-- 12
CREATE TABLE kredit_12(
   nzp_kredx SERIAL NOT NULL,
   nzp_kvar INTEGER NOT NULL,
   nzp_serv INTEGER NOT NULL,
   nzp_kredit INTEGER NOT NULL,
   sum_indolg numeric(14,2) default 0.00,
   sum_dolg numeric(14,2) default 0.00,
   sum_odna12 numeric(14,2) default 0.00,
   sum_perc numeric(14,2) default 0.00,
   sum_charge numeric(14,2) default 0.00,
   sum_outdolg numeric(14,2) default 0.00,
   sum_money numeric(14,2));

CREATE UNIQUE INDEX ixkrx1__12 ON kredit_12(nzp_kredx);
CREATE INDEX ixkrx2__12 ON kredit_12(nzp_kvar, nzp_serv);
CREATE INDEX ixkrx3__12 ON kredit_12(nzp_kredit);