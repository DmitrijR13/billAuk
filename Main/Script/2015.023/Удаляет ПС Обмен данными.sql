SET SEARCH_PATH TO public;


DELETE FROM actions_show WHERE cur_page=321 and nzp_act=188;
DELETE FROM actions_show WHERE cur_page=321 and nzp_act=158;

DELETE FROM actions_lnk WHERE cur_page=321 and nzp_act=188;
DELETE FROM actions_lnk WHERE cur_page=321 and nzp_act=158;

DELETE FROM s_roles WHERE nzp_role=27;

DELETE FROM role_pages WHERE nzp_role=27 and nzp_page=0;
DELETE FROM role_pages WHERE nzp_role=27 and nzp_page=5;
DELETE FROM role_pages WHERE nzp_role=27 and nzp_page=321;

DELETE FROM roleskey WHERE nzp_role=27 and tip=105 and kod=981;
DELETE from pages_show where cur_page=321 OR page_url=321;
DELETE FROM pages WHERE nzp_page=321;



                                                         