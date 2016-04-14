--CENTRAl_DATA

 CREATE TABLE fXXX_data:files_selected
 (
   nzp_file INTEGER,
   nzp_user INTEGER,   
   pref    CHAR(20),
   num     INTEGER,
   comment VARCHAR(100)
 );
 CREATE INDEX fXXX_data:fi_sel_1 ON ftul_data:files_selected(nzp_file, nzp_user, pref);



