
SET SEARCH_PATH TO public;
DELETE FROM role_actions WHERE nzp_role=14 and nzp_page=133 and nzp_act=907;
DELETE FROM actions_show WHERE cur_page=133 and nzp_act=907;
