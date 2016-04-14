SET SEARCH_PATH TO public;

DELETE FROM actions_show WHERE cur_page=311 and nzp_act=90;

DELETE FROM actions_lnk WHERE cur_page=311 and nzp_act=90;

delete from public.role_actions where nzp_page=311 and nzp_role=915 and nzp_act=90;


