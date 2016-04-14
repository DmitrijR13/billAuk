--CENTRAL_DATA

create table "are".kvar_pkodes
(id serial not null,
nzp_kvar integer not null,
area_code integer,
pkod10 integer,
pkod decimal(13) not null,
dat_s date,
dat_po date,
is_actual integer,
changed_by integer not null,
changed_on datetime year to second
);

create unique index "are".ix_kvar_pkodes_1 on kvar_pkodes(id);
create unique index "are".ix_kvar_pkodes_2 on kvar_pkodes(pkod); 
create index "are".ix_kvar_pkodes_3 on kvar_pkodes(nzp_kvar);
create index "are".ix_kvar_pkodes_4 on kvar_pkodes(area_code);