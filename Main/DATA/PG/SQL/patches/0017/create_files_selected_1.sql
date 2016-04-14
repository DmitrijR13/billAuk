--CENTRAl_DATA

CREATE TABLE if not exists fXXX_data.files_selected
 (
   nzp_file INTEGER,
   nzp_user INTEGER,   
   pref    CHARACTER(20),
   num     INTEGER,
   comment VARCHAR(100)
 );
 CREATE INDEX fi_sel_1 ON fXXX_data.files_selected(nzp_file, nzp_user, pref);
