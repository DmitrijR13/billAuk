--Проставить верное значение is_show_empty
UPDATE nftul_kernel.bc_schema SET is_show_empty = 1 WHERE is_requared = 1 AND is_show_empty <> 1;

UPDATE nftul_kernel.bc_schema SET id_bc_row_type = 1 WHERE id_bc_row_type IS NULL; --значение по умолчанию
UPDATE nftul_kernel.bc_schema SET num = 1 WHERE num IS NULL; --значение по умолчанию
UPDATE nftul_kernel.bc_schema SET id_bc_field = 0 WHERE id_bc_field IS NULL; --значение по умолчанию
UPDATE nftul_kernel.bc_schema SET is_requared = 0 WHERE is_requared IS NULL; --значение по умолчанию
UPDATE nftul_kernel.bc_schema SET is_show_empty = 0 WHERE is_show_empty IS NULL; --значение по умолчанию

--Удалить теги у несуществующих форматов
DELETE FROM nftul_kernel.bc_schema s
WHERE NOT EXISTS(SELECT 1 FROM nftul_kernel.bc_types t WHERE t.id = s.id_bc_type);