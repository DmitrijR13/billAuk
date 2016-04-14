
--Таблица создается в центральной data
DROP TABLE fn_percent_dom;
CREATE TABLE fn_percent_dom(
nzp_fp SERIAL NOT NULL,
nzp_payer INTEGER,
nzp_supp INTEGER,
nzp_serv INTEGER,
nzp_area INTEGER,
nzp_geu INTEGER,
perc_ud DECIMAL default 0,
dat_s DATE,
dat_po DATE,
nzp_rs INTEGER default 1 NOT NULL,
nzp_bank INTEGER default -1,
nzp_dom INTEGER default -1,
minpl DECIMAL default 0);

CREATE INDEX i_rsp_dom_01 ON fn_percent_dom(nzp_rs);

CREATE UNIQUE INDEX ix_perc_dom_1 ON fn_percent_dom(nzp_fp);

CREATE INDEX ix_perc_dom_2 ON fn_percent_dom(nzp_payer, nzp_supp);

CREATE INDEX ix_perc_dom_3 ON fn_percent_dom(nzp_payer, nzp_area);

CREATE INDEX ix_perc_dom_9 ON fn_percent_dom(nzp_payer, nzp_supp, nzp_serv);

CREATE INDEX ix_perc_dom_4 ON fn_percent_dom(dat_s, dat_po, nzp_supp, nzp_area);

CREATE INDEX ix_perc_dom_6 ON fn_percent_dom(nzp_payer, nzp_area, nzp_dom);

CREATE INDEX ix_perc_dom_7 ON fn_percent_dom(nzp_payer, nzp_supp, nzp_serv, nzp_dom);

CREATE INDEX ix_perc_dom_8 ON fn_percent_dom(dat_s, dat_po, nzp_supp, nzp_area, nzp_dom);
