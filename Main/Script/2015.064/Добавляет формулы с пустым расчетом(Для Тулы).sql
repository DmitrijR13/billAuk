INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1985', 'Пустой расчет', NULL, NULL, NULL, '8', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1985);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1986', 'Пустой расчет', NULL, NULL, NULL, '6', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1986);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1987', 'Пустой расчет', NULL, NULL, NULL, '2', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1987);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1988', 'Пустой расчет', NULL, NULL, NULL, '5', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1988);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1989', 'Пустой расчет', NULL, NULL, NULL, '4', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1989);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1990', 'Пустой расчет', NULL, NULL, NULL, '1', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1990);
INSERT INTO "nftul_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1991', 'Пустой расчет', NULL, NULL, NULL, '3', '0' WHERE not exists (SELECT 1 FROM "nftul_kernel"."formuls"  WHERE nzp_frm=1991);

