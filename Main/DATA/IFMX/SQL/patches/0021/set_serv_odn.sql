
--database local_kernel;
--database center_kernel;

drop table serv_odn;

create table serv_odn
( nzp_serv      integer,
  nzp_serv_link integer,
  nzp_frm       integer,
  nzp_frm_eqv   integer,
  nzp_prm_mop   integer,
  nzp_prm_mopgr integer,
  nzp_serv_repay integer,
  nzp_frm_repay  integer,
  nzp_prm_repay  integer,
  nzp_prm_repays integer
);

insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (510,  6,990,980,2474,2472,306,1992,1116,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (511,  7,991,981,2049,2471,308,1993,1118,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (512,  8,992,982,2049,2471,309,1994,1119,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (513,  9,993,983,2475,2473,307,1995,1117,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (514, 14,994,984,2049,2471,496,1996,1117,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (515, 25,995,985,2049,2471,310,1997,1120,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (516,210,996,986,2049,2471,497,1998,1120,1122);
insert into serv_odn (nzp_serv,nzp_serv_link,nzp_frm,nzp_frm_eqv,nzp_prm_mop,nzp_prm_mopgr,nzp_serv_repay,nzp_frm_repay,nzp_prm_repay,nzp_prm_repays) values (517, 10,998,988,2049,2471,311,1999,1121,1122);
