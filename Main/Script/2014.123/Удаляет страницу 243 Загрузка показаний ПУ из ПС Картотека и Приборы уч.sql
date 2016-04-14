SET SEARCH_PATH TO public;
delete from role_pages where nzp_page=243;
delete from role_actions where nzp_page=243;
delete from actions_show where cur_page=243;
delete from actions_lnk where cur_page=243;
delete from pages_show where cur_page=243 OR page_url=243;
delete from pages where nzp_page=243;
