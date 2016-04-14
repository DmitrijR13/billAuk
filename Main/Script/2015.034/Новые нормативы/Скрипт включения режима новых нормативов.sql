
----------включение режима новых нормативов
DELETE FROM CENTRAL_data.prm_5  WHERE nzp_prm=1983;
INSERT INTO CENTRAL_data.prm_5  ("nzp", "nzp_prm", "dat_s", "dat_po", "val_prm", "is_actual")
 VALUES ( '0', '1983', '1900-01-01', '3000-01-01', '1', '1');
