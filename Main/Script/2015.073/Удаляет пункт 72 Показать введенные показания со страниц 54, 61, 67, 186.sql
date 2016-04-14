-- Удаляет пункт Показать введенные показания со страниц 54,61,67,186 двух подсистем Картотека и Приборы Учета
SET SEARCH_PATH TO public;

DELETE FROM actions_show WHERE cur_page=54 and nzp_act=72;
DELETE FROM actions_show WHERE cur_page=61 and nzp_act=72;
DELETE FROM actions_show WHERE cur_page=67 and nzp_act=72;
DELETE FROM actions_show WHERE cur_page=186 and nzp_act=72;

DELETE FROM role_actions WHERE nzp_role=13 and nzp_page=67 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=13 and nzp_page=186 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=903 and nzp_page=54 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=903 and nzp_page=61 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=10 and nzp_page=54 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=10 and nzp_page=61 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=10 and nzp_page=67 and nzp_act=72;
DELETE FROM role_actions WHERE nzp_role=10 and nzp_page=186 and nzp_act=72;


