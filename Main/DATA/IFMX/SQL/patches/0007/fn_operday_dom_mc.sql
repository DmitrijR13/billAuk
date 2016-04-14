--DROP TABLE fn_operday_dom_mc;
Центральный PPP_fin_XX

CREATE TABLE fn_operday_dom_mc
(
   nzp_oper SERIAL NOT NULL,
   date_oper DATE,
   nzp_dom INTEGER
   )
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX idx_fn_oplog_dom_mc_1 ON fn_operday_dom_mc(nzp_oper);
CREATE INDEX idx_fn_oplog_dom_mc_2 ON fn_operday_dom_mc(date_oper, nzp_dom);

DROP TABLE "are".fn_reval_dom;

CREATE TABLE "are".fn_reval_dom(
   nzp_reval_dom SERIAL NOT NULL,
   nzp_reval INTEGER NOT NULL,
   dat_oper DATE,
   nzp_payer INTEGER,
   nzp_dom INTEGER,
   nzp_serv INTEGER,
   nzp_payer_2 INTEGER,
   nzp_reval_2 INTEGER,
   sum_reval DECIMAL(14,2) default 0.00 NOT NULL,
   sum_reval_r DECIMAL(14,2) default 0.00 NOT NULL,
   sum_reval_g DECIMAL(14,2) default 0.00 NOT NULL,
   comment CHAR(60),
   nzp_bl INTEGER,
   nzp_area INTEGER,
   nzp_geu INTEGER,
   nzp_user INTEGER,
   dat_when DATE,
   nzp_bank INTEGER default -1)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE PAGE;

CREATE UNIQUE INDEX "are".ix_reval_dom_01 ON "are".fn_reval_dom(nzp_reval_dom);

CREATE INDEX "informix".ix_revt_dom_1 ON "are".fn_reval_dom(dat_oper, nzp_area, nzp_geu, nzp_payer, nzp_dom, nzp_serv);

CREATE INDEX "informix".ix_revt_dom_2 ON "are".fn_reval_dom(dat_oper, nzp_area, nzp_geu, nzp_payer_2, nzp_dom, nzp_serv);

CREATE INDEX "informix".ix_revt_dom_3 ON "are".fn_reval_dom(nzp_payer, nzp_dom, nzp_serv);

CREATE INDEX "informix".ix_revt_dom_4 ON "are".fn_reval_dom(nzp_payer_2, nzp_dom, nzp_serv);

CREATE INDEX "Administ".ix110_dom_5 ON "are".fn_reval_dom(nzp_reval_2);
CREATE INDEX "Administ".ix110_dom_6 ON "are".fn_reval_dom(nzp_reval);

GRANT select, update, insert, delete, index ON fn_reval_dom TO public AS are;