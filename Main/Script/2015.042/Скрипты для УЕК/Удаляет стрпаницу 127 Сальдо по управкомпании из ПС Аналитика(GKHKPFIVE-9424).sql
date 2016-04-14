SET SEARCH_PATH TO public;
delete from actions_show where cur_page=127;
delete from actions_lnk where cur_page=127;
delete from role_pages where nzp_page=127;
delete from role_actions where nzp_page=127;
delete from pages_show where cur_page=127 or page_url=127;
DELETE FROM pages WHERE nzp_page=127;       

