--CENTRAL_FIN_YY
alter table pack add changed_by integer;
alter table pack add changed_on DATETIME YEAR to SECOND;