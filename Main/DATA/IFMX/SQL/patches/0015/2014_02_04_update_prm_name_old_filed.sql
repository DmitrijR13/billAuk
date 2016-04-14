-- центральный kernel
update prm_name set old_field = '0';
update prm_name set old_field = '1' where nzp_prm = 974;

-- локальный kernel
update prm_name set old_field = '0';
update prm_name set old_field = '1' where nzp_prm = 974;