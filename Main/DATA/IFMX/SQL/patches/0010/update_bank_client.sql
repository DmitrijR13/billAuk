--central_data
ALTER TABLE fn_bank ADD nzp_payer_bank INTEGER;

--central_kernel
ALTER TABLE s_payer ADD bik VARCHAR(9);
ALTER TABLE s_payer ADD ks VARCHAR(20);
ALTER TABLE s_payer ADD id_bc_type INTEGER;
ALTER TABLE s_payer ADD city VARCHAR(40) BEFORE	nzp_supp;

--central_fin_YY: fn_sended
ALTER TABLE fn_sended DROP nzp_bank;
ALTER TABLE fn_sended ADD id_bc_file INTEGER;




