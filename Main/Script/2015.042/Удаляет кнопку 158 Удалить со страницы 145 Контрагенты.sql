SET SEARCH_PATH TO public;

DELETE FROM actions_show WHERE cur_page=145 and nzp_act=158;
delete from actions_lnk where cur_page=145 and nzp_act=158;
delete from role_actions where nzp_page=145 and nzp_act=158;

