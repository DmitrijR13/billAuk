--изменения в CENTRAL_data

alter table fsmr_data:kvar add area_code smallint default 0;
                                                                     

DROP TABLE "informix".area_codes;

CREATE TABLE "informix".area_codes(
   code SERIAL NOT NULL,
   nzp_area INTEGER,
   changed_by INTEGER,
   changed_on DATETIME YEAR to MINUTE,
   is_active SMALLINT default 0)
EXTENT SIZE 32 NEXT SIZE 32 LOCK MODE PAGE;

GRANT select, update, insert, delete, index ON area_codes TO public AS informix;

Create unique index ix1_area_codes on area_codes (code);