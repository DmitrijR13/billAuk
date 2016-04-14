SET SEARCH_PATH TO public;
--Удаление справки 40 Карточка регистрации
DELETE FROM actions_show WHERE cur_page=135 and nzp_act=237;
DELETE FROM report WHERE nzp_act=237;
-- Удаление справки 40 Сведения о регистрации ФЛ по МЖ
DELETE FROM actions_show WHERE cur_page=135 and nzp_act=223;
DELETE FROM report WHERE nzp_act=223;

