--central_kernel
--DROP TABLE bc_types;
CREATE TABLE bc_types(
  id SERIAL NOT NULL,
  name_ VARCHAR(100),
  is_active SMALLINT) 
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_types ON bc_types(id);


--central_kernel
--DROP TABLE bc_schema;
CREATE TABLE bc_schema(
  id SERIAL NOT NULL,
  id_bc_type INTEGER,
  id_bc_row_type SMALLINT,
  num INTEGER,
  tag_name VARCHAR(50),
  tag_descr VARCHAR(250),
  id_bc_field INTEGER,
  is_requared SMALLINT,
  is_show_empty SMALLINT)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_schema ON bc_schema(id);


--central_kernel
--DROP TABLE bc_fields;
CREATE TABLE bc_fields(
  id SERIAL NOT NULL,
  name_ VARCHAR(80),
  note_ VARCHAR(150))
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_fields ON bc_fields(id);


--central_kernel
--DROP TABLE bc_row_type;
CREATE TABLE bc_row_type(
  id SERIAL NOT NULL,
  name_ VARCHAR(255))
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_row_type ON bc_row_type(id);


--central_data
--DROP TABLE bc_reestr;
CREATE TABLE bc_reestr(
  id SERIAL NOT NULL,
  date_reestr DATETIME YEAR to MINUTE,
  num_reestr INTEGER,
  nzp_user INTEGER)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_reestr ON bc_reestr(id);


--central_data
--DROP TABLE bc_reestr_files;
CREATE TABLE bc_reestr_files(
  id SERIAL NOT NULL,
  id_bc_reestr INTEGER,
  id_bc_type INTEGER,
  id_payer_bank INTEGER,	
  file_name VARCHAR(255),
  nzp_exc INTEGER,
  is_treaster SMALLINT)
EXTENT SIZE 16 NEXT SIZE 16 LOCK MODE ROW;

CREATE UNIQUE INDEX ix1_reestr_files ON bc_reestr_files(id);
