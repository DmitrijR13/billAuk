--для fsmr_data;
create table efs_reestr (
       nzp_efs_reestr SERIAL NOT NULL,
       file_name CHAR(20),
       date_uchet DATE,
	   packstatus int,
       changed_on DATETIME YEAR to SECOND,
       changed_by INTEGER);     
create unique index ind_efs_reestr  on  efs_reestr(nzp_efs_reestr);    

--для fsmr_fin_XX;
create table efs_pay (
       nzp_pay  SERIAL NOT NULL,
       nzp_efs_reestr INTEGER,
       id_pay DECIMAL(13),
       id_serv CHAR(30),
       ls_num DECIMAL(13),
       summa DECIMAL(14,2),
       pay_date DATE,
       barcode CHAR(30),
       address char(255),
       plpor_num INTEGER,
       plpor_date DATE
);
create index ind_efs_pay on  efs_pay(nzp_efs_reestr, nzp_pay, id_pay, id_serv);  

create table efs_cnt (
       nzp_cnt  SERIAL NOT NULL,
       nzp_efs_reestr INTEGER,
       id_pay DECIMAL(13),
       cnt_num INTEGER,
       cnt_val DECIMAL(10,4),
       cnt_val_be DECIMAL(10,4)
);
create index ind_efs_cnt on  efs_cnt(nzp_efs_reestr, nzp_cnt, id_pay);