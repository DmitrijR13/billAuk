SET SEARCH_PATH TO public;

CREATE OR REPLACE FUNCTION add_to_s_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.s_actions where nzp_act=NEW.nzp_act) then 
raise NOTICE 'row s_actions ignored: nzp_act=%, act_name=%, hlp=%', NEW.nzp_act, NEW.act_name, NEW.hlp; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER s_actions_add BEFORE INSERT 
ON s_actions FOR EACH ROW 
EXECUTE PROCEDURE add_to_s_actions(); 

CREATE OR REPLACE FUNCTION add_to_img_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.img_lnk where cur_page=NEW.cur_page and tip=NEW.tip and kod= NEW.kod) then 
raise NOTICE 'row img_lnk ignored: cur_page=%, tip=%, kod=%', NEW.cur_page, NEW.tip, NEW.kod; 
return NULL; 
end if; 
return NEW;
END; 
$body$; 
CREATE TRIGGER img_lnk_add BEFORE INSERT 
ON img_lnk FOR EACH ROW 
EXECUTE PROCEDURE add_to_img_lnk(); 

CREATE OR REPLACE FUNCTION add_to_page_links() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.page_links where (page_from=NEW.page_from OR (page_from is null and NEW.page_from is NULL )) 
and (group_from=NEW.group_from OR (group_from is null and NEW.group_from is NULL )) 
and (page_to=NEW.page_to OR (page_to is null and NEW.page_to is NULL )) 
and (group_to=NEW.group_to OR (group_to is null and NEW.group_to is NULL ))) then 
raise NOTICE 'row page_links ignored: page_from=%, group_from=%, page_to=%, group_to=%', NEW.page_from, NEW.group_from, NEW.page_to, NEW.group_to ; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER page_links_add BEFORE INSERT 
ON page_links FOR EACH ROW 
EXECUTE PROCEDURE add_to_page_links(); 

CREATE OR REPLACE FUNCTION add_to_report() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.report where nzp_act=NEW.nzp_act ) then  
raise NOTICE 'row report ignored: nzp_act=%, name=%', NEW.nzp_act, NEW.name; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER report_add BEFORE INSERT 
ON report FOR EACH ROW 
EXECUTE PROCEDURE add_to_report();

CREATE OR REPLACE FUNCTION add_to_s_roles() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.s_roles where nzp_role=NEW.nzp_role) then 
 raise NOTICE 'row s_roles ignored: nzp_role=%, role=%', NEW.nzp_role, NEW.role; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER s_roles_add BEFORE INSERT 
ON s_roles FOR EACH ROW 
EXECUTE PROCEDURE add_to_s_roles(); 

CREATE OR REPLACE FUNCTION add_to_roleskey() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.roleskey where nzp_role=NEW.nzp_role and tip=NEW.tip and kod=NEW.kod) then 
raise NOTICE 'row roleskey ignored: nzp_role=%, tip=%, kod=%', NEW.nzp_role, NEW.tip, NEW.kod; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER roleskey_add BEFORE INSERT 
ON roleskey FOR EACH ROW 
EXECUTE PROCEDURE add_to_roleskey();  


-- Триггер(ы). Необходимы для того, чтобы избежать ограничения внешнего ключа  
CREATE OR REPLACE FUNCTION add_to_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.pages where nzp_page=NEW.nzp_page) then 
raise NOTICE 'row pages ignored: nzp_page=%, page_name=%, page_url=%', NEW.nzp_page, NEW.page_name, NEW.page_url; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER pages_add BEFORE INSERT 
ON pages FOR EACH ROW 
EXECUTE PROCEDURE add_to_pages() ; 

CREATE OR REPLACE FUNCTION add_to_actions_show() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.actions_show where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then 
raise NOTICE 'row actions_show ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER actions_show_add BEFORE INSERT 
ON actions_show FOR EACH ROW 
EXECUTE PROCEDURE add_to_actions_show(); 

CREATE OR REPLACE FUNCTION add_to_actions_lnk() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.actions_lnk where cur_page=NEW.cur_page and nzp_act=NEW.nzp_act) then  
raise NOTICE 'row actions_lnk ignored: cur_page=%, nzp_act=%', NEW.cur_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER actions_lnk_add BEFORE INSERT 
ON actions_lnk FOR EACH ROW 
EXECUTE PROCEDURE add_to_actions_lnk(); 

CREATE OR REPLACE FUNCTION add_to_role_actions() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.role_actions where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page and nzp_act=NEW.nzp_act) then 
raise NOTICE 'row role_actions ignored: nzp_role=%, nzp_page=%, nzp_act=%', NEW.nzp_role, NEW.nzp_page, NEW.nzp_act; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER role_actions_add BEFORE INSERT 
ON role_actions FOR EACH ROW 
EXECUTE PROCEDURE add_to_role_actions(); 

CREATE OR REPLACE FUNCTION add_to_role_pages() RETURNS trigger LANGUAGE plpgsql AS $body$ 
BEGIN 
if exists(select * from public.role_pages where nzp_role=NEW.nzp_role and nzp_page=NEW.nzp_page) then 
 raise NOTICE 'row role_pages ignored: nzp_role=%, nzp_page=%', NEW.nzp_role, NEW.nzp_page; 
return NULL; 
end if; 
return NEW; 
END; 
$body$; 
CREATE TRIGGER role_pages_add BEFORE INSERT 
ON role_pages FOR EACH ROW 
EXECUTE PROCEDURE add_to_role_pages(); 
-- ПС Дожники
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (206, '~/kart/bill/report.aspx', 'Отчеты', 'Отчеты', 'переходит к списку отчетов, выполняемых по всему банку данных', 75, 75);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (322, '~/debitor/find/finddebt.aspx', 'Поиск по долгу', 'Поиск по долгу', 'отображает форму для поиска по задолженности', 30, 30);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (323, '~/debitor/find/finddeals.aspx', 'Поиск по делам', 'Поиск по делам', 'отображает форму для поиска по делам должников', 30, 30);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (324, '~/debitor/list/debitors.aspx', 'Должники', 'Должники', 'отображает выбранный список лицевых счетов с задолженностями', 40, 40);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (325, '~/debitor/list/deals.aspx', 'Дела', 'Дела', 'отображает выбранный список дел должников', 40, 40);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (326, '~/debitor/deal/deal.aspx', 'Дело', 'Дело', 'переходит на форму выбранного дела', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (327, '~/debitor/deal/agreement.aspx', 'Соглашение', 'Соглашение о рассрочке на оплату долга', 'переходит на форму выбранного соглашения о предоставлении рассрочки на оплату долга', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (328, '~/debitor/deal/debtclaim.aspx', 'Иск', 'Иск', 'переходит на форму выбранного иска', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (329, '~/debitor/deal/debtchange.aspx', 'Изменение задолженности', 'Изменение задолженности', 'переходит на форму корректировки суммы долга', null, 326);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (330, '~/debitor/settings/debtsettings.aspx', 'Настройки', 'Настройки', 'переходит на форму настроек системы должников', 270, 270);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES (332, '~/debitor/operations/dealoperations.aspx', 'С делами', 'Групповые операции с делами', 'переходит на форму групповых операций с выбранным списком дел', 74, 325);


--Заполнение таблицы s_actions
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (1, 'Выполнить поиск', 'выполняет поиск после заполнения требуемых полей поиска');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (2, 'Очистить шаблон', 'очищает ранее заполненные поля шаблона');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (3, 'Открыть данные', 'открывает указанные данные на просмотр или для редактирования');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (4, 'Добавить запись', 'добавляет запись в список');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (5, 'Обновить список', 'обновляет ранее выбранный список');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (6, 'Очистить список', 'очищает ранее заполненные поля формы');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (7, 'Показать карту', 'показывает интерактивную карту Яndex');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (8, 'На печать', 'открывает форму для печати');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (9, 'Блокировка', 'блокирует или разблокирует запись в зависимости от текущего состояния');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (10, 'Запустить', 'запускает фоновый процесс обработки заданий');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (11, 'Приостановить', 'останавливает фоновый процесс обработки заданий');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (12, 'Удалить все', 'удаляет все выбранные записи');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (13, 'Удалить сессию', 'удаляет все активные сессии выбранного пользователя');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (14, 'Открыть показания', 'открывает форму с информацией о показаниях прибора учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (15, 'Сбросить пароль', 'удаляет пароль выбранного пользователя');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (16, 'Установить значение', 'устанавливает значение параметра в заданном интервале времени');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (17, 'Удалить значение', 'удалить значение параметра в заданном интервале времени');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (18, 'Добавить недопоставку', 'добавляет недопоставку по услуге');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (19, 'Удалить недопоставку', 'удаляет недопоставку по услуге');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (20, 'Все параметры', 'показывает все параметры: и имеющие действующие значения и не имеющие установленных значений');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (21, 'Установить услугу', 'назначает период оказания услуги, поставщика и формулу расчета услуги, действующие в этот период');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (22, 'Закрыть услугу', 'сохраняет информацию о том, что услуга не оказывается в выбранном периоде времени');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (23, 'Добавить жильца', 'добавляет нового жильца в поквартирную карточку');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (24, 'Создать лицевой счет', 'создает новый лицевой счет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (25, 'Создать дом', 'создает новый дом');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (26, 'Удалить запись', 'удаляет выбранную запись');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (61, 'Сохранить изменения', 'сохраняет введенные данные');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (64, 'Удалить ПУ', 'удаляет запись прибора учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (65, 'Выполнить подсчет', 'выполняет подсчет агрегированных данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (66, 'Обновить данные', 'выполняет обновление текущих данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (67, 'Показать все значения', 'показывает все значения одного параметра');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (68, 'Обновить списки', 'обновляет списки доступных управляющей организаций, участков, услуг, поставщиков, банков данных, которые используются для задания прав доступа роли');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (69, 'Получить отчет', 'формирует отчет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (70, 'Расчет', 'выполняет расчет начислений');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (71, 'Показать утвержденные показания', 'показывает утвержденные показания приборов учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (72, 'Показать введенные показания', 'показывает введенные показания приборов учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (73, 'Выгрузить в Excel', 'выгружает данные в файл электронных таблиц (MS Excel)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (74, 'Перейти в архив', 'переходит в режим просмотра архивных данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (75, 'Выйти из архива', 'выходит из режима просмотра архивных данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (76, 'Показать периоды', 'переходит к просмотру всех периодов действия услуги, и соответствующих им поставщиков и формул расчета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (77, 'Показать параметры', 'переходит к просмотру параметров выбранной услуги');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (78, 'Копировать', 'Копировать данные');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (79, 'Вставить', 'Вставить скопированные данные');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (80, 'Показать ЛС', 'Показать лицевые счета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (81, 'Добавить наряд-заказ', 'Добавить наряд-заказ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (82, 'Включить в группу', 'Включить лицевые счета в группу');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (83, 'Исключить из группы', 'Исключить лицевые счета из группы');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (84, 'К списку заявок', 'Перейти к списку заявок лицевого счета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (85, 'Создать пачку', 'Создать новую пачку с оплатами');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (86, 'Закрыть пачку', 'Закрыть пачку оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (87, 'Добавить оплату', 'Добавить новую оплату в открытую пачку');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (88, 'Загрузить оплаты', 'Загружает электронный файл оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (89, 'Найти лицевой счет', 'Переходит к шаблону поиска по адресу');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (90, 'Удалить оплату', 'Удаляет выбранные оплаты из пачки');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (91, 'Отменить распред.', 'Отменяет распределение оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (92, 'Удалить пачку', 'Удаляет пачку');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (93, 'Распределить', 'Выполняет распределение оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (94, 'Добавить заявку', 'добавляет новую заявку');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (95, 'Очистить списки', 'удаляет выбранные списки всех пользователей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (96, 'Добавить пользователя', 'добавляет нового пользователя');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (97, 'Отправить', 'отправляет данный наряд-заказ исполнителю');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (98, 'Отметить получение', 'отмечает данный наряд-заказ как полученный');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (99, 'Сформировать', 'формирует недопоставки по указанным условиям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (100, 'Выставить признак', 'Выставляет признак формирования недопоставки по невыполненным наряд-заказам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (101, 'Снять признак', 'Снимает признак формирования недопоставки по невыполненным наряд-заказам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (102, 'Удалить', 'Удалить запись из журнала');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (103, 'Показать параметры', 'переходит к просмотру параметров выбранной записи');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (104, 'Реквизиты печати', 'переходит к просмотру реквизитов управляющей организации');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (107, 'Перечислить', 'переходит к перечислению оплат подрядчикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (108, 'Расчет сальдо', 'выполняет расчет сальдо поставщика');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (109, 'Добавить врем. убытие', 'добавляет временное убытие выделенным жильцам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (110, 'Загрузить показания', 'Загружает электронный файл с показаниями приборов учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (111, 'Оператор', 'показывает показания, введенные оператором');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (112, 'С сайта', 'показывает показания, загруженные из электронного файла с показаниями ПУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (113, 'Перезапустить хостинг', 'Перезапустить хостинг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (114, 'Поместить в портфель', 'Поместить в портфель');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (115, 'Убрать из портфеля', 'Исключает выбранную квитанцию об оплате из портфеля');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (116, 'Отменить платежи', 'Отменяет все платежи портфеля текущим операционным днем');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (118, 'Исправить', 'Выполняет попытку исправления ошибок и повторно распределяет оплату');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (120, 'Распределить', 'Выполняет распределение выбранной оплаты');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (122, 'Сгенерировать ЛС', 'Выполняет генерацию лицевых счетов по выбранному дому');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (123, 'Сгенерировать ПУ', 'Выполняет генерацию приборов учета по выбранному списку лицевых счетов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (124, 'Контроль распред.', 'Контроль распределения оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (125, 'Ошибки распределения', 'Выполняет поиск оплат, распределение которых не равно сумме оплаты, и помещает их в корзину с ошибкой "Сумма оплаты не соответствует сумме распределения"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (126, 'Ошибки в оплатах', 'Выявляет учтенные в лицевых счетах оплаты, по которым отсутвует квитанция об оплате, и аннулирует их');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (127, 'Открыть квитанцию', 'Открыть квитанцию');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (128, 'Версия для печати', 'Отображает версию для печати');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (129, 'Обновить адреса', 'Обновляет адресное пространство в центральном банке');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (130, 'Закрыть', 'Закрывает наряд-заказ для подрядчика');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (131, 'Выполнить проверки', 'Выполняет проверки на возможность закрытия расчетного месяца');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (132, 'Закрыть месяц', 'Выполняет проверки на возможность закрытия расчетного месяца и закрывает расчетный месяц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (133, 'Переместить вверх', 'Перемещает выбранную услугу на одну позицию вверх в списке');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (134, 'Переместить вниз', 'Перемещает выбранную услугу на одну позицию вниз в списке');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (135, 'Редактировать адреса', 'Отображает блок адреса на редактирование');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (136, 'Отменить', 'Отменяет получение наряда-заказа');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (137, 'Обновить справочники', 'Обновляет справочники из основного банка данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (140, 'Удалить', 'Удаляет список к перечислению');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (141, 'Добавить', 'Добавляет новый приказ на перечисление');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (142, 'Добавить список', 'Формирует новый список к перечислению за текущий расчетный месяц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (143, 'Обновить страницу', 'Обновляет данную страницу');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (145, 'Добавить', 'Добавляет соглашение с подрядчиком');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (146, 'Удалить', 'Удаляет соглашение с подрядчиком');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (147, 'Распределить', 'Выполняет распределение сумм финансирования по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (148, 'Отменить распред.', 'Выполняет отмену распределения сумм финансирования для выбранного приказа');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (149, 'Удалить приказ', 'Выполняет удаление приказа на перечисление');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (150, 'Новое сообщение', 'Создает новое сообщение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (151, 'Отправить', 'Отправляет сообщение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (152, 'Удалить', 'Удаляет сообщение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (153, 'Загрузить', 'Загрузить');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (154, 'Загрузить акт', 'Загружает акт о фактической поставке из файла');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (155, 'Учесть акт', 'Учитывает данные акта по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (156, 'Загрузить характеристику', 'Загружает техническую характеристику жилого фонда из файла');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (157, 'Учесть характеристику', 'Учитывает техническую характеристику жилого фонда по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (158, 'Удалить', 'Удаляет выбранную запись');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (159, 'К распределению', 'Ставит признак готовности к распределению по лицевым счетам недопоставки СУПГ ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (160, 'Распределить', 'Выполняет распределение недопоставок СУПГ по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (161, 'Удалить кассовый', 'Уадалить кассовый план');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (162, 'Удалить помесячный', 'Удалить помесячное распределение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (163, 'Загрузить', 'Загрузить файл');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (164, 'Рассчитать', 'Выполняет расчет статистики по начислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (165, 'Скопировать', 'Создает новую карточку плановой работы и копирует данные из текущей карточки');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (166, 'Разобрать', 'Выполняет разбор');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (167, 'Присвоить пл-код', 'Выпоняет присвоение платежного кода лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (168, 'Файл выгрузки', 'Формирует файл выгрузки недопоставок для передачи его в систему КомПлат 2.0');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (169, 'Добавить', 'Добавить новую запись');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (170, 'Сохранить', 'Сохранить изменения');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (171, 'Перераспределить', 'Выполняет перераспределение оплат на выделенные открытые лицевые счета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (172, 'Расчет ср.расх. ИПУ', 'Выполняет расчет средних расходов индивидуальных приборов учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (173, 'Выполнить', 'выполняет обновление данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (174, 'Выполнить', 'выполняет операцию');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (175, 'Жилец', 'Показывает показания введенные жильцом');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (176, 'Добавить формулу', 'Добавляет новую формулу расчета начислений по услуге');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (177, 'Расчет расхода', 'Рассчитывает расход ОДПУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (178, 'Перенести на ЛС', 'Переходит к форме переноса суммы с выбранного лицевого счета на другой лицевой счет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (179, 'Новая операция', 'Очищает форму, подготавливая ее к выполнению новой операции переноса суммы между лицевыми счетами');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (180, 'Реестр операций', 'Переходит к форме просмотра реестра операций по изменению сальдо');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (181, 'Учесть оплаты', 'Выполняет учет в начислениях распределенных оплат');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (182, 'Создать пачку', 'Создает пачку с переплатами');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (184, 'Перенести', 'Переносит все лицевые счета выбранных домов в новую УК');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (185, 'Новый пакет', 'Новый пакет для системы платежных поручений банк-клиент');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (186, 'Удалить пакет', 'Удаляет пакет из системы платежных поручений банк-клиент');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (187, 'Загрузить адреса', 'Загрузить адресное пространство');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (188, 'Выгрузить', 'Выгрузить файл');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (189, 'Открыть выгрузку', 'Открыть выгрузку');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (190, 'Ввод по услугам', 'Открывает форму для ввода изменений по услугам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (192, 'Открыть соглашение', 'Открывает выбранное соглашение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (193, 'Открыть иск', 'Открывает выбранный иск');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (194, 'Изменить долг', 'Открывает форму для изменения задолженности');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (196, 'Настроить ключи', 'Выполняет настройку уникальных ключей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (197, 'Сформировать документы', 'Формирует пакет документов для иска');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (198, 'Создать дело', 'Создает новое дело для должника');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (199, 'Закрыть дело', 'Меняет статус дело на Закрыто');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (200, 'Открыть дело', 'Открывает дело должника');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (201, 'Справка нотариусу', 'Справка нотариусу');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (203, 'Справка в суды', 'Справка в суды');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (204, 'Справка на приватизацию', 'Справка на приватизацию');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (205, 'Справка о прописке', 'Справка о прописке');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (206, 'Лицевой счет', 'Лицевой счет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (207, 'Финансово-лицевой счет', 'Финансово-лицевой счет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (208, 'Справка о составе семьи', 'Справка о составе семьи');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (209, 'Справка по начислениям', 'Справка по начислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (210, 'Справка о задолженности', 'Справка о задолженности');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (211, 'Выписка из домовой книги', 'Выписка из домовой книги');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (212, 'Сальдовая ведомость по услугам 5.10', 'Сальдовая ведомость по услугам 5.10');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (213, 'Сальдовая ведомость по услугам 5.20', 'Сальдовая ведомость по услугам 5.20');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (214, 'Заявление о регистрации по месту пребывания', 'Заявление о регистрации по месту пребывания');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (215, 'Заявление о снятии с регистрационного учета по месту жительства', 'Заявление о снятии с регистрационного учета по месту жительства');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (216, 'Заявление о регистрации по месту жительства', 'Заявление о регистрации по месту жительства');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (217, 'Финансовый лицевой счет №__', 'Финансовый лицевой счет №__');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (218, 'Адресный листок убытия', 'Адресный листок убытия');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (219, 'Адресный листок прибытия', 'Адресный листок прибытия');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (220, 'Сведения о регистрации граждан и снятии с регистрационного учета', 'Сведения о регистрации граждан и снятии с регистрационного учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (221, 'Список юношей, подлежащих постановке на воинский учет', 'Список юношей, подлежащих постановке на воинский учет');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (222, 'Реестр граждан, сменивших или получивших удостоверение личности', 'Реестр граждан, сменивших или получивших удостоверение личности');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (223, 'Сведения о регистрации по месту жительства (Форма РФЛ1)', 'Сведения о регистрации гражданина по месту жительства по форме РФЛ1');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (224, 'Листок статистического учета прибытия', 'Листок статистического учета прибытия');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (225, 'Реестр граждан', 'Универсальный реестр граждан, построенный по выбранному списку карточек жильцов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (226, 'Заявление о выдаче/замене паспорта', 'Заявление о выдаче/замене паспорта');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (227, 'Листок статистического учета выбытия', 'Листок статистического учета выбытия');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (228, 'Генератор по параметрам', 'Генератор отчетов по параметрам жилья');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (229, 'Карточка аналитического учета', 'Карта аналитического учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (230, 'Сверка расчетов с жильцом', 'Сверка расчетов с жильцом');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (231, 'Расшифровка по домам - общая', 'Расшифровка по домам - общая');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (232, 'Справка по поставщикам коммунальных услуг (Форма 2)', 'Справка по поставщикам коммунальных услуг (Форма 2)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (233, 'Справка о наличии долга', 'Справка о наличии долга');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (234, 'Справка по отключениям подачи коммунальных услуг', 'Справка по отключениям подачи коммунальных услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (235, 'Справка для предъявления в суд', 'Справка для предъявления в суд');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (236, 'Справка по лицевому счету (Excel)', 'Справка по лицевому счету (Excel)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (237, 'Карточка регистрации', 'Карточка регистрации');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (238, 'Архивная справка', 'Архивная справка');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (239, 'Справка по поставщикам коммунальных услуг (Форма 1)', 'Справка по поставщикам коммунальных услуг (Форма 1)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (240, 'Извещение за месяц', 'Извещение за месяц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (241, 'Поквартирная карточка', 'Поквартирная карточка');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (242, 'Справка с места жительства', 'Справка с места жительства');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (243, 'Справка по запросам', 'Справка по запросам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (244, 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги', 'Оплата гражданами-получателями коммунальных услуг за поставленные услуги');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (245, 'Информация  по расчетам с населением', 'Информация  по расчетам с населением');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (246, 'Акт сверки по энергоснабжению', 'Акт сверки по энергоснабжению');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (247, 'Генератор по начислениям', 'Генератор по начислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (248, 'Справка о начислениях по квартирным приборам учета', 'Справка о начислениях по квартирным приборам учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (249, 'Сводная ведомость по нормативам потребления КУ', 'Сводная ведомость по нормативам потребления КУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (250, 'Отчет о жилом фонде', 'Отчет о жилом фонде');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (251, 'Расшифровка по домам - начислено', 'Расшифровка по домам - начислено');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (253, 'Список должников с указанием срока задолженности', 'Список должников с указанием срока задолженности');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (254, 'Сведения о задолженности', 'Сведения о задолженности');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (255, 'Справка о начислении платы по видам услуг', 'Справка о начислении платы по видам услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (256, 'Расшифровка по домам с ОДПУ', 'Расшифровка по домам с ОДПУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (257, 'Ведомость оплаты', 'Ведомость оплаты');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (258, 'Ведомость разовых начислений', 'Ведомость разовых начислений');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (259, 'Формирование квитанций', 'Формирование квитанций для списка лицевых счетов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (260, 'Протокол расчета общедомового поправочного коэффициента', 'Протокол расчета общедомового поправочного коэффициента');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (261, 'Карточка аналитического учета по услуге "содержание жилья"', 'Карта аналитического учета по услуге "содержание жилья"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (263, '1.5 Список заявок', '1.5 Список заявок');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (264, '2.1 Количество нарядов - заказов по услугам', '2.1 Количество нарядов - заказов по услугам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (265, '2.2 Количество нарядов - заказов по подрядчикам', '2.2 Количество нарядов - заказов по подрядчикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (266, 'Сальдовая оборотная ведомость (10.14.3)', 'Сальдовая оборотная ведомость (10.14.3)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (267, 'Сальдовая оборотная ведомость по поставщикам (10.14.1)', 'Сальдовая оборотная ведомость по поставщикам (10.14.1)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (268, 'Справка для незарегистрированного собственника', 'Справка для незарегистрированного собственника');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (269, 'Лицевой счет без долга', 'Лицевой счет без долга');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (270, 'Справка по услугам группы "содержание жилья"', 'Справка по услугам группы "содержание жилья"');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (271, 'Справка по отключениям подачи коммунальных услуг по домам', 'Справка по отключениям подачи коммунальных услуг по домам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (272, 'Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ', 'Справка по отключениям подачи коммунальных услуг сводная по ЖЭУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (273, '3.1 Сведения по отключениям услуг по поставщикам', 'Сведения по отключениям услуг по поставщикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (274, '3.2 Сведения по отключениям услуг', 'Сведения по отключениям услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (275, '3.3 Акты по отключениям услуг', 'Акты по отключениям услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (276, '2.3 Список нарядов-заказов по неисправностям', 'Список нарядов-заказов по неисправностям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (277, '1.4 Список невыполненных нарядов-заказов к концу периода', '1.4 Список невыполненных нарядов-заказов к концу периода');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (278, '1.2 Приложение к информации, полученной ОДДС', '1.2 Приложение к информации, полученной ОДДС');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (279, '1.1 Информация, полученная ОДДС', '1.1 Информация, полученная ОДДС');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (280, '2.4 Количество переадресаций заявок, принятых ОДДС', '2.4 Количество переадресаций заявок, принятых ОДДС');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (281, '1.3.1 Список сообщений, зарегистрированных ОДДС', '1.3.1 Список сообщений, зарегистрированных ОДДС');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (282, '1.3.2 Список сообщений, зарегистрированных ОДДС(опрос)', '1.3.2 Список сообщений, зарегистрированных ОДДС(опрос)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (283, 'Список жильцов модифицируемый', 'Список жильцов, построенный по выбранному списку карточек жильцов с возможностью выбора полей для отображения');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (284, 'Архивная справка-2', 'Архивная справка');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (285, 'Сверка поступлений за день', 'Сверка поступлений за день');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (286, 'Сверка поступлений за период', 'Сверка поступлений за период');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (287, 'Сводная информация по распределению оплат за период', 'Сводная информация по распределению оплат за период');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (288, 'Реестр счетчиков по лицевым счетам', 'Реестр счетчиков по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (289, 'Начисления по поставщикам', 'Начисления по поставщикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (290, 'Сальдовая оборотная ведомость по услугам для всех поставщиков', 'Сальдовая оборотная ведомость по услугам для всех поставщиков');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (291, 'Сводная ведомость начислений и оплат в разрезе поставщиков', 'Сводная ведомость начислений и оплат в разрезе поставщиков');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (292, '4.16. Данные приборов учета по жилым домам', '4.16. Данные приборов учета по жилым домам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (293, '4.18. Отчет по расходу на дома', '4.18. Отчет по расходу на дома');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (294, 'Протокол рассчитанных значений ОДН', 'Протокол рассчитанных значений ОДН');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (295, 'Рассогласование с паспортисткой', 'Рассогласование с паспортисткой');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (296, 'Печать', 'формирует отчет о сформированныъ недопоставках');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (300, 'Выписка из ЛС о поданных показаниях ИПУ', 'Выписка из ЛС');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (301, 'Справка на приватизацию2', 'Справка на приватизацию2');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (302, 'Протокол расчета значений ОДН расширенный', 'Протокол расчета ОДН');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (303, 'Выгрузка в Кассу 3.0', 'Выгрузка в Кассу 3.0');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (304, 'Платежи.Ежедневная информация по платежам за ЖКУ по г.Губкину', 'Ежедневная информация по платежам за ЖКУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (305, 'Сверка поступлений за месяц', 'Сверка поступлений за месяц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (306, 'Сальдо в банк', 'Сальдо в банк');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (307, 'Состояние текущих начислений по домам', 'Состояние текущих начислений по домам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (308, 'Итоги оплат по домам (ЕПД)', 'Итоги оплат по домам (ЕПД)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (309, 'Справка по поставщикам коммунальных услуг (Форма 3)', 'Справка по поставщикам коммунальных услуг (Форма 3)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (310, 'Справка о начислении платы по видам услуг (Форма 2)', 'Справка о начислении платы по видам услуг (Форма 2)');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (311, 'Статистика состояния жилищного фонда', 'Статистика состояния жилфонда');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (312, 'Расписка в получении документов жильца', 'Расписка в получении документов жильца');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (313, 'Свидетельство о регистрации по месту пребывания', 'Свидетельство о регистрации по месту пребывания');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (314, 'Справка с места жительства', 'Справка с места жительства');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (315, 'Выписка из лицевого счета', 'Выписка из лицевого счета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (316, 'Заявление на регистрацию', 'Заявление на регистрацию');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (317, 'Статистика начислений по лицевым счетам', 'Статистика начислений по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (318, 'Статистика начислений по домам', 'Статистика начислений по домам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (319, 'Статистика начислений по участкам', 'Статистика начислений по участкам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (320, 'Выгрузка показаний ПУ', 'Выгрузка показаний ПУ');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (321, 'Выгрузка начислений в банк', 'Выгрузка начислений в банк');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (323, 'Выгрузка начислений в орган социальной защиты населения', 'Выгрузка начислений в орган социальной защиты населения');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (324, '50.1 Сальдовая ведомость', '50.1 Сальдовая ведомость');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (325, '50.2 Ведомость должников', '50.2 Ведомость должников');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (327, 'Сверка характеристик лицевых счетов и домов', 'Протокол сверки характеристик лицевых счетов и домов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (329, 'Настройка расчета', 'Настройка расчета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (330, 'Справка на проживание и состав семьи', 'Справка на проживание и состав семьи');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (331, 'Справка о смерти', 'Справка о смерти');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (332, 'Информация о временно зарегистрированных', 'Информация о временно зарегистрированных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (333, 'Информация о собственниках', 'Информация о собственниках');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (334, 'Список жильцов для военкомата', 'Список жильцов для военкомата');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (335, 'Выписка на жилое помещение', 'Выписка на жилое помещение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (336, 'Выписка из домовой книги для горгаза', 'Выписка из домовой книги для горгаза');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (340, '3.70 Сводный отчет по начислениям', '3.70 Сводный отчет по начислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (341, '3.71 Сводный отчет по поступлениям', '3.71 Сводный отчет по поступлениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (342, 'Выписка по счетчикам', 'Выписка по счетчикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (346, 'Реестр квитанций по домам', 'Реестр квитанций по домам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (347, 'Сведения о должниках', 'Сведения о должниках');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (348, 'Справка по должникам', 'Справка по должникам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (349, 'Справочник поставщиков', 'Справочник поставщиков');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (350, 'Сведения по поступлениям и перечислениям', 'Сведения по поступлениям и перечислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (501, 'Шаблон по адресам', 'учитывает при поиске данные, введенные в шаблоне поиска адресов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (502, 'Шаблон по параметрам', 'учитывает при поиске данные, введенные в шаблоне поиска характеристик жилья');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (503, 'Шаблон по начислениям', 'учитывает при поиске данные, введенные в шаблоне поиска начислений и расходов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (504, 'Шаблон по жильцам', 'учитывает при поиске данные, введенные в шаблоне поиска житилей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (505, 'Шаблон по показаниям', 'учитывает при поиске данные, введенные в шаблоне поиска показаний приборов учета');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (506, 'Шаблон недопоставок', 'учитывает при поиске данные, введенные в шаблоне поиска недопоставок');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (507, 'Шаблон ОДН', 'учитывает при поиске данные, введенные в шаблоне поиска адресов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (508, 'Шаблон по услугам', 'учитывает при поиске данные, введенные в шаблоне поиска по услугам и поставщикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (509, 'Шаблон по заявкам', 'учитывает при поиске данные, введенные в шаблоне поиска по заявкам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (510, 'Шаблон по группам', 'учитывает при поиске данные, введенные в шаблоне поиска по группам лицевых счетов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (512, 'Шаблон по плановым работам', 'учитывает при поиске данные, введенные в шаблоне поиска по плановым работам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (513, 'Шаблон по долгам', 'учитывает при поиске данные, введенные в шаблоне поиска по долгам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (514, 'Шаблон по делам', 'учитывает при поиске данные, введенные в шаблоне поиска по делам должников');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (520, 'По месяцам', 'выполняет выборку данных в помесячном разрезе');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (521, 'По услугам', 'выполняет выборку данных в разрезе услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (522, 'По поставщикам', 'выполняет выборку данных в разрезе поставщиков');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (523, 'По формулам', 'выполняет выборку данных разрезе формул расчета услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (528, 'Сальдовые', 'выполняет отображение сальдовых сумм');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (530, 'Сейчас на сайте', 'показывает только пользователей, находящихся сейчас на сайте');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (531, 'Заблокированные', 'показывает только заблокированные записи');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (532, 'В очереди', 'показывает зарегистрированные процессы, находящиеся в очереди на исполнение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (533, 'Выполняется', 'показывает выполняющиеся процессы');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (534, 'Выполнен', 'показыает успешно выполненные процессы');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (535, 'Есть ошибки', 'показывает процессы с ошибками в результате выполнения');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (540, 'Перейти к делу', 'переходит к форме дела');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (541, 'Сформировать пачки', 'Формирует пачки');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (542, 'Портал', 'показывает показания, введенные с портала');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (543, 'Утвердить', 'Переносит выбранные показания в утвержденные');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (544, 'Учесть к перечислению', 'Учесть к перечислению');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (545, 'Закрыть месяц', 'Запускает процессы подготовки данных и закрывает месяц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (548, 'Закрыть день', 'Закрывает операционный день');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (549, 'Вернуться назад', 'Возвращается на предыдущий операционный день');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (550, 'Перераспределить', 'Повторно распределяет оплаты, считающиеся распределенными, но без расщепления по услугам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (551, 'Скопировать в собст.', 'Копирует данные по жильцу в список собственников');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (552, 'Сделать ответственным', 'проверяет наличие помеченной карточки, задает вопрос на подтверждение');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (554, 'Обновить статус', 'Обновляет статус выбранной пачке');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (555, 'Скачать', 'Скачать файл');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (601, 'Сортировать по адресу', 'сортирует список по адресу');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (602, 'Сортировать по лс', 'сортирует список по лицевым счетам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (603, 'Сортировать по улице', 'сортирует список по улице');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (604, 'Сортировать по управл. орг.', 'сортирует список по управляющим организациям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (605, 'Сортировать по услуге', 'сортирует список по услугам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (606, 'Сортировать по поставщику', 'сортирует список по поставщикам');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (607, 'Сортировать по ФИОДР', 'сортирует список по людям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (608, 'Сортировать по логину', 'сортирует список по логину');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (609, 'Сортировать по имени пользователя', 'сортирует список по имени пользователя');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (610, 'На просмотр', 'открывает данные на просмотр');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (611, 'Для изменения', 'открывает данные для редактирования');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (612, 'Сортировать по дате последнего посещения', 'сортирует пользователей по убыванию даты последнего посещения');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (701, 'Выводить по 20 записей', 'выводить список по 20 записей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (702, 'Выводить по 50 записей', 'выводить список по 50 записей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (703, 'Выводить по 100 записей', 'выводить список по 100 записей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (704, 'Выводить все записи', 'выводить все записи на странице');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (705, 'Выводить по 10 записей', 'выводить список по 10 записей');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (721, 'Управляющая компания / Улица / Дом', 'отображает данные в разрезе управляющих компаний');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (722, 'Банк данных / Участок / Улица / Дом', 'отображает данные в разрезе банков данных');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (723, 'Улица / Дом', 'отображает данные в разрезе улиц');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (724, 'Поставщик / Услуга / Формула', 'отображает данные в разрезе поставщиков услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (725, 'Услуга / Поставщик / Управляющая компания / Участок ', 'отображает данные в разрезе услуг');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (726, 'Управляющая компания / Поставщик / Услуга / Формула', 'отображает данные в разрезе УК и поставщиков');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (851, 'Убывших', '');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (852, 'Историю', '');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (853, 'Удаленных', 'позоволяет просматривать удаленные карточки жильцов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (854, 'Архив', 'позоволяет просматривать карточки жильцов из архива');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (861, 'Карточка жильца', 'открывает карточку жильца');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (862, 'Периоды убытия', 'открывает периоды временного убытия жильца');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (863, 'Досье', 'открывает досье жильца');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (870, 'Стандартный формат', 'описывает формат загрузки информации в систему (характеристики жилого фонда, начисления и др.');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (871, 'Генерация П.кодов', 'Запускает фоновую процедуру генерации платежных кодов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES (875, 'Пересчитать комиссию', 'Запускает фоновую процедуру');

                                  
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (31, 513, 1, 1, 513);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (31, 514, 1, 1, 514);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 69, 0, 0, 69);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 285, 2, 5, 285);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 286, 2, 5, 286);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 287, 2, 5, 287);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 292, 2, 5, 292);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 293, 2, 5, 293);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 303, 2, 5, 303);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 305, 2, 5, 305);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 323, 2, 5, 323);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 347, 2, 5, 347);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (206, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 1, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 2, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 501, 1, 1, 501);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (322, 514, 1, 1, 514);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 1, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 2, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 501, 1, 1, 501);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (323, 513, 1, 1, 513);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 1, 3, 0, 1544);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 198, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (324, 200, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (325, 1, 3, 0, 1545);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (325, 200, 0, 0, 200);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 61, 0, 0, 4);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 192, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 193, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 194, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 198, 3, 0, 2092);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 199, 0, 0, 5);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 200, 3, 0, 2093);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (326, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 26, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 61, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 192, 3, 0, 2096);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (327, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 158, 0, 0, 4);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 170, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 193, 3, 0, 2097);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 197, 0, 0, 3);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (328, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 170, 0, 0, 2);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 194, 3, 0, 2098);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (329, 540, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 61, 0, 0, 1);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 610, 2, 1, 610);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (330, 611, 2, 1, 611);
INSERT INTO actions_show (cur_page, nzp_act, act_tip, act_dd, sort_kod) VALUES (332, 174, 0, 0, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (206, 69, 206);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (322, 1, 324);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (322, 2, 322);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (323, 1, 325);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (323, 2, 323);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (324, 200, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (324, 198, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (325, 200, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 192, 327);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 193, 328);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 199, 326);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 194, 329);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES (326, 61, 326);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES (28, 'Должники', 31, 14);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES (982, 'Должники - настройки', 0, 982);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES (0, 3, 28, 'folder_home.png');

INSERT INTO roleskey (nzp_role, tip, kod) VALUES (28, 105, 982);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 0);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 5);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 31);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 75);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 206);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 322);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 323);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 324);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 325);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 326);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 327);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 328);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 329);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (28, 332);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES (982, 330);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 5, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 5, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 513, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 31, 514, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 69, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 347, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 206, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 501, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 322, 514, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 2, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 501, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 323, 513, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 198, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 324, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 325, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 325, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 192, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 193, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 194, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 198, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 199, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 200, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 326, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 192, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 327, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 193, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 197, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 328, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 194, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 329, 540, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (28, 332, 174, null);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES (982, 330, 611, null);

-- Договоры ЖКУ
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (65, '~/kart/sprav/new_contracts.aspx', 'Договоры ЖКУ', 'Договоры ЖКУ', 'переходит на форму для просмотра и управления договорами на оказание жилищно-коммунальных услуг', 73, 73);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (65, 169, 15, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (65, 169, 86);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (65, null, null, 73);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act) VALUES  (942, 65, 169);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 65, 169, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 65);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 65);

--Доступные услуги
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (114, '~/kart/serv/new_availableservices.aspx', 'Доступные услуги', 'Доступные услуги', 'переходит на страницу доступных услуг', 73, 73);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (115, '~/kart/serv/new_availableservice.aspx', 'Доступная услуга', 'Доступная услуга', 'переходит на страницу доступная услуга', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 76, 76, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 701, 701, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 702, 702, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 703, 703, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (114, 705, 705, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 26, 26, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (115, 76, 76, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (114, 5, 114);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (114, 76, 115);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (115, 21, 115);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (115, 26, 115);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 114, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 114, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 115, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (31, 115, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 115, 611, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 114);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (31, 114);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 115);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (31, 115);

-- перечень услуг
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (142, '~/kart/serv/new_spisservdom.aspx', 'Перечень услуг', 'Перечень услуг дома', 'переходит на список услуг для выбранного дома', 71, 71);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 80, 80, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (142, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 5, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 22, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 4, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 61, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 70, 142);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (142, 80, 50);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 80, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 142, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 142, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 142, 61, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 142);

--счета контрагентов

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (143, '~/finances/new_payerrequisites.aspx', 'Счета контрагентов', 'Счета контрагентов', 'переходит к списку расчетных счетов выбранного контрагента', 290, 290);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (143, 26, 1899, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 4, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 61, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 5, 232);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (143, 26, 232);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 26, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 61, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 143, 4, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 143, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 143, 5, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 143);

--Сальдо по перечислениям
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (144, '~/kart/charge/new_distrib_dom_supp.aspx', 'Сальдо по перечислениям', 'Сальдо по перечислениям', 'переходит на форму с данными о распределении оплат', 72, 72);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 1, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 108, 108, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 124, 124, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 875, 875, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 701, 701, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 702, 702, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 703, 703, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (144, 705, 705, 2, 3);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 1, 144);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 875, 144);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (144, 108, 144);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (144, null, null, 235);


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 144, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 144, 705, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 144, 108, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 144, 875, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 144);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 144);

--Контрагенты
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (145, '~/finances/sprav/new_contractors.aspx', 'Контрагенты', 'Контрагенты', 'отображает справочник контрагентов', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 610, 610, 2, 1);
--INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 77, 77, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 4, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 5, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 61, 145);
--INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 158, 145);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 77, 28);

INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (145, null, null, 290);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 4, 611);
--INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 145, 611, null);
delete from role_actions where nzp_page=145 and nzp_role=805;
delete from roleskey where nzp_role=15 and tip=105 and kod=805;
delete from s_roles where nzp_role=805;
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 145);
DELETE FROM actions_show WHERE cur_page=145 and nzp_act=158;

--редактирование Договоров ЖКУ
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (86, '~/kart/sprav/edit_new_contracts.aspx', 'Редактирование договора ЖКУ', 'Редактирование договора ЖКУ', 'переходит на форму редактирования договора ЖКУ', null, null);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (920, 'Изменить обл. действия', 'Изменяет область действия выбранного договора');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 170, 170, 0,0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 920, 920, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (86, 103, 103, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (86, 170, 65);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (65, 169, 86);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (86, 103, 175);
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 920, 'edit.png');


INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 86, 920, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 86, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 86, 103, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 86, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (933, 86, 920, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 86);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 86);

--Услуги

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (116, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Перечень услуг', 'переходит на форму перечня услуг', 70, 70);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (117, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Групповые операции с услугами', 'переходит на список услуг для выполнения групповой операции над выбранными лицевыми счетами', 74, 41);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (118, '~/kart/serv/newspisserv.aspx', 'Перечень услуг', 'Групповые операции с услугами для выбранных домов', 'переходит на список услуг для выполнения групповой операции над лицевыми счетами выбранных домов', 74, 42);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (119, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Поставщики и формулы расчета', 'переходит на список периодов действия поставщиков и формул расчета для выбранной услуги и лицевого счета', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (132, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Групповые операции с услугой', 'переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и выбранных лицевых счетов', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (140, '~/kart/serv/newserv.aspx', 'Поставщики и формулы расчета', 'Групповые операции с услугой для выбранных домов', 'переходит в окно добавления или удаления периода действия поставщиков и формул расчета для выбранной услуги и лицевых счетов выбранных домов', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (141, '~/finances/newpayertransfer_supp.aspx', 'Перечисления контрагентам', 'Перечисления контрагентам', 'переходит на форму перечисления контрагентам средств по договорам', 235, 235);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 76, 76, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 77, 77, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (116, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (117, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (117, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (118, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (118, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 70, 70, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (119, 76, 76, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (132, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 21, 21, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 22, 22, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (140, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (141, 170, 170, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 5, 116);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 76, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (116, 77, 176);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (117, 5, 117);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (118, 5, 118);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 5, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 21, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (119, 22, 119);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (132, 21, 132);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (132, 22, 132);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (140, 21, 140);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (140, 22, 140);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (141, 170, 141);

--Заполнение таблицы page_links
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 2, 2);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 30, 30);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 40, 40);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 72, 72);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 73, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 74, 74);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 75, 75);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 77, 77);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 150, 150);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 160, 160);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 270, 270);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, null, 291, 291);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, 41, null, 41);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 104, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 108, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, 42, null, 42);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, 56, null, 56);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 70, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 70, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 71, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, null, 68);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, 68, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, 69, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, 81, 81, 81);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, 82, 82, 82);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (175, null, 175, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 69);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, 69, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (78, 78, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, 91, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 153, null, 153);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 154, null, 154);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 164, null, 164);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (201, 235, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (353, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (273, 273, null, 273);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 276, null, 276);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 277, null, 277);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (256, 290, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (355, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (279, 276, null, 276);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (221, null, null, 301);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (302, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (302, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (303, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (303, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (316, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (316, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (324, 324, 74, 324);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (325, 325, 74, 325);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (352, 352, 352, 352);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (353, null, 356, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 226, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (31, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (33, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (35, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (36, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (37, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (38, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (39, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (41, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (42, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (45, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (49, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (51, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (51, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (52, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (53, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (55, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (55, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (59, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (59, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (64, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (64, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (68, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (82, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (95, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (95, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (97, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (97, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (98, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (98, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (99, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (99, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (101, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (102, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (103, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (105, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (106, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (107, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (109, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (111, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (111, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (112, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (112, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (122, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (122, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (123, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (123, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (124, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (124, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (126, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (130, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (133, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (133, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (137, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (137, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (138, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (139, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (151, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (152, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (153, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (154, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (155, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (161, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (163, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (164, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (165, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (165, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (166, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (166, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (167, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (167, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (168, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (169, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (170, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (172, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (172, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (174, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (177, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (177, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (179, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (180, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (181, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (182, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (183, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (184, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (184, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (185, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (185, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (186, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (186, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (188, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (188, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (189, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (190, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (190, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (191, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (191, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (192, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (192, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (193, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (194, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (195, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (198, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (199, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (200, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (203, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (205, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (206, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (207, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (209, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (210, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (211, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (212, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (213, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (214, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (215, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (216, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (217, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (218, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (219, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (220, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (221, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (222, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (223, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (224, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (225, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 4, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 53, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 56, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 42, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 3, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 33, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 82, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 361, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 35, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 278, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 999, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 37, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 1, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 292, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 106, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 5, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 36, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 107, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 50, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 75, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 41, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 72, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 40, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 81, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 195, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 30, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 353, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 340, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 34, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 2, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 31, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 38, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (226, null, 206, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (227, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (228, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (229, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (230, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (231, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (232, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (233, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (234, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (238, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (239, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (240, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (240, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (241, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (242, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (242, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (243, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (244, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (244, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (245, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (247, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (248, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (249, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (251, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (252, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (253, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (255, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (256, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (257, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (258, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (259, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (260, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (260, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (261, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (262, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (262, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (265, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (266, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (267, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (268, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (269, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (271, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (272, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (273, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (274, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (277, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (278, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (279, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (280, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (281, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (281, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (282, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (283, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (284, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (285, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (286, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (287, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (288, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (289, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 254, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 255, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (297, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (298, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (342, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 5, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 44, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 256, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 4, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 2, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 270, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 999, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 152, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 151, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 77, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 293, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 268, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 155, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 161, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 150, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 3, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 271, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 73, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 1, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (343, null, 160, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (204, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (81, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (56, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (96, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (176, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (173, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, null, 91);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (162, null, null, 56);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (246, null, null, 246);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 197);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (91, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (163, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (194, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (208, null, null, 78);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (197, null, null, 193);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (164, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 202, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (178, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (null, 199, null, 198);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (62, null, null, 68);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 70, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (171, null, 71, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (65, 65, 65, 65);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (144, null, null, 235);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (145, null, null, 290);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (65, null, null, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (60, null, null, 73);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, 6, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (119, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (187, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (93, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (69, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES (50, null, 117, null);



INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, null, 71);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, 184, null);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (119, null, 6, null);



INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 76, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 116, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 116, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 119, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 119, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 116, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 119, 70, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 117, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 117, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 118, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 118, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 119, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 132, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 21, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 22, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 140, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (806, 141, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 116);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 116);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 119);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 119);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 117);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 118);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 132);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 140);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 141);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 141);

--страницы Договоры ЕРЦ

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (60, '~/finances/contracts/new_contract_details.aspx', 'Договоры ЕРЦ', 'Договоры ЕРЦ', 'переходит на страницу договоров ЕРЦ', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 169, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (60, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 170, 60);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 169, 60);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (60, 158, 60);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (60, null, null, 73);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (942, 60, 158, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 60);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 60);

--Добавляет страницу 146 Список проводок в ПС Картотека

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (146, '~/kart/charge/listprovs.aspx', 'Список проводок', 'Список проводок', 'переходит на форму списка проводок', 70, 70);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (922, 'Перезапись проводок', 'Перезапись проводок');

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 922, 'recreate_pack.png');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (146, 922, 922, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (146, 922, 146);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (872, 'Редактирование проводок', 0, 872);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (872, 146, 922, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 146);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 872);

--кнопка обновить на странице 268 Расчетный месяц
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 5, 5, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 5, 268);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 268, 5, null);

--добавляет кнопку в корзине 919 перекинуть в тек фин год

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (919, 'Перенести в тек.фин.год', 'Переносит оплаты из корзины прошлых фин.гоодов в текущий');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (258, 919, 919, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (258, 919, 258);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 919, 'panel_window.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 258, 919, null);

--Добавляет страницу 67 Групповые показания ПУ в ПС Приборы Учета
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (67, '~/kart/counter/spisval.aspx', 'Показания групповых ПУ', 'Показания групповых приборов учета', 'переходит на список показаний групповых приборов учета для выбранного дома', 71, 71);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (111, 'Оператор', 'показывает показания, введенные оператором');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (112, 'С сайта', 'показывает показания, загруженные из электронного файла с показаниями ПУ');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 71, 71, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 72, 72, 2, 3);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 111, 111, 2, 4);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 112, 112, 2, 4);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (67, 14, 1633, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (67, 5, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (67, 61, 67);



INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 72, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 71, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 67, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 67, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 67, 61, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 67);

--Добавляет страницу 92 Групповые ПУ в ПС Картотека, Аналитика и Приборы Учета
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (92, '~/kart/counter/counters.aspx', 'Групповые ПУ', 'Групповые приборы учета', 'переходит на список групповых приборов учета для выбранного дома', 71, 71);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 3, 3, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 4, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 64, 64, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (92, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 3, 93);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 4, 93);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 5, 92);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 14, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (92, 64, 92);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 64, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 92, 4, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 92, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 92, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 92, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 93, 4, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 92);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 92);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 92);

--Добавляет страницу 93 Групповой прибор учета в ПС Приборы Учета
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 3, 1630, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (93, 4, 1631, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 14, 67);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (93, 61, 93);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 93, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 93, 3, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 93);

--Добаялет страницу 187 Общеквартирный ПУ в ПС Приборы Учета
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (187, '~/kart/counter/countercard.aspx', 'Общеквартирный ПУ', 'Общеквартирный прибор учета', 'переходит в карточку общеквартирного прибора учета', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 14, 14, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 3, 1757, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 4, 1758, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 158, 158, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (187, 169, 169, 0, 0);

INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (187, null, null, 70);
INSERT INTO page_links (page_from, group_from, page_to, group_to) VALUES  (187, null, null, 71);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 3, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 14, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (13, 187, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 187, 169, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 158, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (903, 187, 169, 611);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (13, 187);



--Переименовывает страницу 82 с Поставщики услуг на Договоры
UPDATE pages Set page_menu='Договоры', page_name='Договоры', hlp='Договоры' WHERE nzp_page=82;

--Изменяет наименование страницы 168 Тарифы на Параметры расчета
UPDATE pages Set page_menu='Параметры расчета', page_name='Параметры расчета', hlp='переходит на форму с информацией о параметрах расчета' WHERE nzp_page=168;

--удаляет кнопку 197 Сформировать документы из ПС Должники
DELETE FROM actions_show WHERE cur_page=328 and nzp_act=197;

DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=328 and nzp_act=197;

--Удаляет страницу 243 Загрузка показаний ПУ из ПС Картотека и Приборы уч
delete from role_pages where nzp_page=243;
delete from role_actions where nzp_page=243;
delete from actions_show where cur_page=243;
delete from actions_lnk where cur_page=243;
delete from pages_show where cur_page=243 OR page_url=243;
delete from pages where nzp_page=243;

--Удаляет стрпаницу 127 Сальдо по управкомпании из ПС Аналитика
delete from actions_show where cur_page=127;
delete from actions_lnk where cur_page=127;
delete from role_pages where nzp_page=127;
delete from role_actions where nzp_page=127;
delete from pages_show where cur_page=127 or page_url=127;
DELETE FROM pages WHERE nzp_page=127; 

-- действия для страницы 310 Выгрузка банк клиент 
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 185, 185, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 186, 186, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (310, 189, 189, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 185, 310);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 186, 310);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (310, 189, 314);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 189, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 185, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 186, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (978, 310, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (978, 310);

--Паспортистка. Не выходит таблица при редактировании реквизитов л/с GKHKPFIVE-9880
Delete from  fbill_data.prm_5 where nzp_prm = 1997;

insert into fbill_data.prm_5 (nzp, nzp_prm, dat_s, dat_po, val_prm, is_actual, nzp_user, dat_when)
values(0, 1997, '01.01.2004', '01.01.3000','1',1,1,now());

SET SEARCH_PATH TO public;

UPDATE pages Set page_menu='Корректировка начислений', page_name='Корректировка начислений', hlp='переходит на форму с информацией о корректировке начислений' WHERE nzp_page=165;


--Добавляет кнопки 18 Добавить недопоставку и 19 Удалить не допоставку на страницу 55 Недопоставки в ПС Учет Претензий
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (18, 'Добавить недопоставку', 'добавляет недопоставку по услуге');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (19, 'Удалить недопоставку', 'удаляет недопоставку по услуге');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (55, 18, 18, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (55, 19, 19, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (55, 18, 55);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (55, 19, 55);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (934, 55, 18, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (934, 55, 19, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (935, 55, 18, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (935, 55, 19, 611);

DELETE FROM role_actions WHERE nzp_role=919 and nzp_page=55 and nzp_act=611;

--Добавляет страницы 28 Параметры котрагентов, 29 Параметр котрагентов в ПС Финансы
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (28, '~/finances/sprav/contragentparams.aspx', 'Параметры контрагента', 'Параметры контрагента', 'Переходит на страницу Параметры контрагента', null, null);
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (29, '~/kart/prm/prm.aspx', 'Параметр контрагента', 'Параметр контрагента', 'переходит на форму просмотра и редактирования выбранного параметра контрагента', null, null);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (145, 77, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 20, 20, 1, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (28, 67, 67, 3, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 16, 16, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 17, 17, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (29, 67, 67, 3, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (145, 77, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 5, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 61, 28);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (28, 67, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 5, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 16, 29);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (29, 17, 29);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 145, 77, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 20, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 67, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 28, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 16, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 17, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 29, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 28);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 29);

--Удаляет кнопку 158 Удалить со страницы 145 Контрагенты
DELETE FROM actions_show WHERE cur_page=145 and nzp_act=158;
delete from actions_lnk where cur_page=145 and nzp_act=158;
delete from role_actions where nzp_page=145 and nzp_act=158;

--Удаляет кнопку 168 Файл выгрузки со страницы 220 Формирование недопоставок
delete from public.role_actions where nzp_act=168 and nzp_role in (919,950) and nzp_page=220;

-- Добавляет Для изменения на странице 244 Рассрочка
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (244, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (244, 61, 61, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 244, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 244, 61, 611);

-- Страница Подготовка расчета пени
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (22, '~/admin/process/preparefirstcalcpeni.aspx', 'Подготовка расчета пени', 'Подготовка первого запуска расчета пени', 'Подготовка первого запуска расчета пени', 77, 77);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (905, 'Подготовить расчет', 'Подготавливает первый запуск расччета пени');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (22, 905, 905, 0, 0);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 905, 'calc32.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 22, 905, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 22);
-- Переименование
update s_actions set act_name='По договорам', hlp='выполняет выборку данных в разрезе договоров' where nzp_act=522;
update pages set page_menu='Преиоды перерасчета', page_name='Периоды перерасчета' where nzp_page=296;
-- добавление Список лицевых счетов в ПС Финансы
insert into public.role_pages (nzp_role, nzp_page) values (15,41);

-- Страница Приоритеты услуг
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (269, '~/finances/settings/servpriority.aspx', 'Приоритеты услуг', 'Приоритетные услуги при учете оплат', 'отображает список услуг, имеющих приоритет при распределении оплаты', 270, 270);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 4, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 61, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 133, 3, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 134, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 26, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (269, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 4, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 61, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 26, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 133, 269);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (269, 134, 269);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 269, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 4, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 133, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 134, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 26, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 269, 611, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 269);
-- Страница 89 Быстрый ввод оплат
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (89, '~/finances/pack/addpack.aspx', 'Быстрый ввод оплат', 'Добавление пачки оплат в режиме быстрый ввод', 'отображает форму добавления пачки оплат в режиме быстрый ввод', 77, 77);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (917, 'Быстрый ввод оплат', 'Быстрый ввод оплат');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 917, 'add_new.png');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 86, 10, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 92, 20, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 917, 40, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 86, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 92, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 170, 89);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 917, 87);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 86, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 92, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 89, 917, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 89);

-- Страница 14 Перенос домов
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (14, '~/admin/settings/transfer_homes.aspx', 'Перенос домов', 'Перенос домов из одного локального банка в другой', 'Перенос домов из одного локального банка в другой', 77, 77);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (880, 'Перенести', 'Перенос дома в другой локальный банк');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (14, 880, 880, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (14, 880, 14);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 880, 'min_window.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 14, 880, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 14);


-- копки для быстрого ввода оплат в ПС Финансы
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (909, 'Автомат. ввод', 'Автоматизированный ввод');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (917, 'Быстрый ввод оплат', 'Быстрый ввод оплат');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 909, 909, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 917, 917, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 909, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 917, 87);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 909, 'add_new.png');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 917, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 909, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 917, null);

-- Страница быстрый ввод оплат 
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (87, '~/finances/pack/quickaddpackls.aspx', 'Быстрый ввод оплат', 'Быстрый ввод оплат', 'Быстрый ввод оплат', null, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (916, 'Сохр. и закрыть пачку', 'Сохраняет и закрывает пачку');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (87, 916, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (87, 90, 90, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (89, 917, 40, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (87, 916, 87);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (87, 90, 87);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (89, 917, 87);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 916, 'close_pack.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 87, 916, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 87, 90, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 87);

-- Кнопка 122 Сгенерировать ЛС
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (974, 'Генерация лицевых счетов', 0, 974);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 974);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (42, 122, 7, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (42, 122, 260);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 122, 'save.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 42, 122, null);
delete from public.role_actions where nzp_role=901 and nzp_page=42 and nzp_act=122;

-- удаляет кнопку 543 Утвердить
delete from public.actions_lnk where nzp_act=543;
delete from public.actions_show where nzp_act=543;
delete from public.role_actions where nzp_act=543;
delete from public.s_actions where nzp_act=543;
-- Удаляет элемент из раскрывающегося списка Показать введенные показания 
delete from public.actions_lnk where nzp_act=72;
delete from public.actions_show where nzp_act=72;
delete from public.role_actions where nzp_act=72;
delete from public.s_actions where nzp_act=72;
-- Страница 24 Автоматизированный ввод
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (24, '~/finances/pack/autoaddpackls.aspx', 'Автоматизированный ввод', 'Автоматизированный ввод', 'Автоматизированный ввод оплат при помощи штрих-кодов', null, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (909, 'Автомат. ввод', 'Автоматизированный ввод');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (24, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (24, 158, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (311, 909, 909, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (24, 158, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (24, 170, 24);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (311, 909, 24);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 909, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 311, 909, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 24, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (915, 24, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (915, 24);
 -- Страница 268 Расчетный месяц
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (268, '~/kart/charge/calcmonth.aspx', 'Расчетный месяц', 'Расчетный месяц', 'отображает информацию о текущем расчетном месяце и предоставляет функции изменения расчетного месяца', 77, 77);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 131, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 132, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 611, 611, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (268, 5, 5, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 131, 268);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 132, 268);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (268, 5, 268);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (814, 'Проверки перед закрытие месяца', 0, 814);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 814);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 131, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (814, 268, 5, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (814, 268);

-- Страница отключение начисление Пени

INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (20, '~/kart/charge/disablechargepeni.aspx', 'Отключение начисления пени', 'Отключение начисления пени', 'Отключает начисление пени по договорам', 77, 77);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (20, 169, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (20, 158, 2, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (20, 158, 20);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (20, 169, 20);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (898, 'Отключение начисления пени', 0, 898);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (898, 20, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (898, 20, 169, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (898, 20);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 898);

-- удаление страницы 363 смена адреса
delete from public.actions_show where cur_page=363;
delete from public.actions_lnk where cur_page=363;
delete from public.role_actions where nzp_page=363;
delete from public.role_pages where nzp_page=363;
delete from public.pages_show where cur_page=363 or page_url=363;
delete from public.pages where nzp_page=363;
delete from public.roleskey where nzp_role=10 and tip=105 and kod=813;
delete from public.s_roles where nzp_role=813;
-- удаление страницы 309 Смена УК
delete from public.actions_show where cur_page=309;
delete from public.actions_lnk where cur_page=309;
delete from public.role_actions where nzp_page=309;
delete from public.role_pages where nzp_page=309;
delete from public.pages_show where cur_page=309 or page_url=309;
delete from public.pages where nzp_page=309;

-- Статистика по начислению
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (361, '~/kart/charge/statcharge_supp.aspx', 'Статистика по начислениям', 'Статистика по начислениям', 'переходит в окно со сводными данными о начислениях', 72, 72);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (164, 'Рассчитать', 'Выполняет расчет статистики по начислениям');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (361, 1, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (361, 164, 2, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (361, 1, 361);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (361, 164, 361);
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 164, 'calc32.png');
UPDATE img_lnk Set img_url='calc32.png' WHERE cur_page=0 and tip=2 and kod=164;
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 361, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (11, 361, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (921, 361, 164, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 361);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (11, 361);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 72);

-- 260 Генерация Лицевых счетов
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (260, '~/kart/adres/gendomls.aspx', 'Генерация лицевых счетов', 'Генерация лицевых счетов', 'отображает форму для генерации лицевых счетов по выбранному дому', null, null);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (260, 122, 122, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (260, 611, 123, 2, 1);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (260, 122, 260);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 260, 122, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (974, 260, 611, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (974, 260);
delete from public.role_pages where nzp_role=901 and nzp_page=260;
-- Отчет 323 Выгрузка в орган соц защиты
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (323, 'Выгрузка начислений в орган социальной защиты населения', 'Выгрузка начислений в орган социальной защиты населения');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (206, 323, 323, 2, 5);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 206, 323, null);
INSERT INTO report (nzp_act, name, file_name) VALUES  (323, 'Выгрузка начислений в орган социальной защиты населения', 'UnloadingToKassa.frx');
-- Удаление отчета 349 Справочник поставщиков
delete from public.role_actions where nzp_act=349 and nzp_role=10;

-- удаление страницы 304 Перенос переплат
delete from public.actions_show where cur_page=304;
delete from public.actions_lnk where cur_page=304;
delete from public.role_actions where nzp_page=304;
delete from public.role_pages where nzp_page=304;
delete from public.pages_show where cur_page=304 or page_url=304;
delete from public.pages where nzp_page=304;
-- Страница  366 Управление переплатами
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (366, '~/finances/operations/overpaymentmanager.aspx', 'Управление переплатами', 'Управление переплатами', 'переходит на страницу Управление переплатами', 77, 77);
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (926, 'Прервать процесс', 'Прервать процесс');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (927, 'Отобрать переплаты', 'Отобрать переплаты');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 926, 'close_zakaz.png');
INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 927, 'add_new_ls.png');
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 66, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 93, 4, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 170, 8, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 926, 16, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (366, 927, 12, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 93, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 170, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 66, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 926, 366);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (366, 927, 366);
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (864, 'Управление переплатами', 0, 864);
INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (15, 105, 864);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 66, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 93, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 170, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 926, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (864, 366, 927, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (864, 366);
-- Изменение страницы 165 Корректировка начислений
Delete from role_actions where nzp_role=901 and nzp_page=165 and nzp_act=178;
delete from role_actions where nzp_role=10 and nzp_page=165 and nzp_act=180;
delete from public.role_actions where nzp_role=901 and nzp_act=158 and nzp_page=165;


-- Страница 347 История событий дома
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (347, '~/kart/adres/dom_events.aspx', 'События', 'История событий дома', 'История событий дома', 71, 71);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 347);

-- Страница 349 Параметры
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (349, '~/kart/prm/params.aspx', 'Параметры', 'Параметры', 'переходит к справочнику параметров', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 170, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 169, 2, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (349, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 169, 349);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 158, 349);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (349, 170, 349);

INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (993, 'Справочник параметров', 0, 993);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 158, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (993, 349, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (993, 349);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 993);

-- Страница 21 Типы Убытия
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (21, '~/kart/sprav/typestempdeparture.aspx', 'Типы убытия', 'Типы временного убытия граждан', 'Типы временного убытия граждан', 73, 73);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (21, 169, 1, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (21, 170, 170, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 21, 169, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 21, 170, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 21);

-- Удаляет страницу 10 Загрузка расхода по счетчикам
delete from public.actions_show where cur_page=10;
delete from public.actions_lnk where cur_page=10;
delete from public.role_actions where nzp_page=10;
delete from public.role_pages where nzp_page=10;
delete from public.pages_show where cur_page=10 or page_url=10;
delete from public.pages where nzp_page=10;
-- добавляет кнопку Удалить ЛС

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (931, 'Удалить лицевой счет', 'Удалить лицевой счет');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (41, 931, 931, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (41, 931, 41);

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 931, 'delete.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 41, 931, 611);


-- Страиница 263 Групповой ввод характеристик жилья
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (263, '~/kart/prm/groupprm.aspx', 'Групповой ввод характеристик жилья', 'Групповой ввод характеристик жилья', 'отображает форму для группового ввода характеристик жилья', 74, 41);

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 5, 5, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 61, 61, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 610, 610, 2, 1);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (263, 611, 611, 2, 1);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (263, 5, 263);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (263, 61, 263);

UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 61, 611);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (901, 263, 610, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (901, 263);

-- Отчеты в Паспортиске на странице 134
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 69, 69, 0, 0);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 69, null);

INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (247, 'Генератор по начислениям', 'Генератор по начислениям');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (228, 'Генератор по параметрам', 'Генератор отчетов по параметрам жилья');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (259, 'Формирование квитанций', 'Формирование квитанций для списка лицевых счетов');
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (295, 'Рассогласование с паспортисткой', 'Рассогласование с паспортисткой');

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 228, 228, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 247, 247, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 259, 259, 2, 5);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (134, 295, 295, 2, 5);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 247, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 228, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 259, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 134, 295, null);

INSERT INTO report (nzp_act, name, file_name) VALUES  (247, 'Генератор по начислениям', 'web_247.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (228, 'Генератор по параметрам', 'web_otchet_param.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (259, 'Формирование квитанций', 'Web_259.frx');
INSERT INTO report (nzp_act, name, file_name) VALUES  (295, 'Рассогласование с паспортисткой', 'web_295.frx');

-- Удаление пункта меню Сальдо по постащику в ПС Аналитика со страницы Поставщики услуг
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (135, 878, 16, 2, 5);

DELETE FROM actions_show WHERE cur_page=133 and nzp_act=878;

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (14, 135, 878, null);



-- удаляет страницу 299 Изменение сальдо
delete from public.actions_show where cur_page=299;
delete from public.actions_lnk where cur_page=299;
delete from public.role_actions where nzp_page=299;
delete from public.role_pages where nzp_page=299;
delete from public.pages_show where cur_page=299 or page_url=299;
delete from public.pages where nzp_page=299;

-- добавляет кнопку 915 Добавить запись для сущ жильца
INSERT INTO s_actions (nzp_act, act_name, hlp) VALUES  (915, 'Доб. зап. для сущ. жил.', 'Добляет запись для существующего жильца');
update s_actions set act_name='Доб. зап. для сущ. жил.' where nzp_act=915;

INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (162, 915, 4, 0, 0);

DELETE FROM actions_show WHERE cur_page=162 and nzp_act=4;

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (162, 915, 91);

DELETE FROM actions_lnk WHERE cur_page=162 and nzp_act=4;

INSERT INTO img_lnk (cur_page, tip, kod, img_url) VALUES  (0, 2, 915, 'add_new.png');

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (904, 162, 915, 611);

DELETE FROM role_actions WHERE nzp_role=904 and nzp_page=162 and nzp_act=4;

-- Поиск по адресу для ПС Финансы
delete from public.pages_show where cur_page=31 and page_url=257;
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 31, 1, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 31, 2, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 31);
-- Страница 113 Экспорт и импорт параметров
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (113, '~/admin/files/import_export_param.aspx', 'Экспорт и импорт параметров', 'Экспорт и импорт параметров', 'переходит на страницу экспорта и импорта параметров', 270, 270);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (113, 188, 188, 0, 0);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (113, 163, 163, 0, 0);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (113, 163, 113);
INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (113, 188, 113);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 113, 188, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (12, 113, 163, null);
INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (12, 113);

--роль 946 Картотека - распределение недопоставок УПГ
INSERT INTO s_roles (nzp_role, role, page_url, sort) VALUES  (946, 'Картотека (Распределение недопоставок УПГ)', 0, 946);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 611, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 160, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (946, 220, 296, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (946, 220);

INSERT INTO roleskey (nzp_role, tip, kod) VALUES  (10, 105, 946);
-- Загрузка начислений от сторонних поставщиков
INSERT INTO pages (nzp_page, page_url, page_menu, page_name, hlp, up_kod, group_id) VALUES  (12, '~/exchange/upload/supp_charges.aspx', 'Загрузка начислений', 'Загрузка начислений сторонних поставщиков', null, 270, 270);
INSERT INTO actions_show (cur_page, nzp_act, sort_kod, act_tip, act_dd) VALUES  (12, 158, 3, 0, 0);

INSERT INTO actions_lnk (cur_page, nzp_act, page_url) VALUES  (12, 158, 12);

INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (10, 12, 158, null);

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (10, 12);


--список платежей вместо списка начислений в ПС Финансы (123 страница вместо 122)
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 5, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 610, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 701, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 702, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 703, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 601, null);
INSERT INTO role_actions (nzp_role, nzp_page, nzp_act, mod_act) VALUES  (15, 123, 602, null);

DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=5;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=70;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=520;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=521;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=522;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=523;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=528;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=922;
DELETE FROM role_actions WHERE nzp_role=15 and nzp_page=122 and nzp_act=925;

INSERT INTO role_pages (nzp_role, nzp_page) VALUES  (15, 123);

DELETE FROM role_pages WHERE nzp_role=15 and nzp_page=122;

--Удаляет страницу 206 Отчеты из  ПС Должники
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=69;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=343;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=344;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=345;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=347;
DELETE FROM role_actions WHERE nzp_role=28 and nzp_page=206 and nzp_act=610;

DELETE FROM role_pages WHERE nzp_role=28 and nzp_page=206;

--Удаляет пункт отчеты из  ПС Аналитика
DELETE FROM role_pages WHERE nzp_role=11 and nzp_page=75;
DELETE from role_pages where nzp_role=11 and nzp_page=206;

-- перенос Лицевых счетов дома из пункта Дом
update pages set up_kod=40, group_id=40  where nzp_page=57;
delete from pages_show where cur_page=57 or page_url=57;


INSERT INTO pages_show (cur_page,page_url,up_kod,sort_kod)
SELECT DISTINCT a.nzp_page, b.nzp_page, COALESCE(b.up_kod,0), b.nzp_page
FROM pages a, pages b, page_links  pl
WHERE (pl.page_from = a.nzp_page or pl.group_from = a.group_id or (pl.page_from is null and pl.group_from is null))
and (pl.page_to = b.nzp_page or pl.group_to = b.group_id)
and (select count(*) from pages_show ps where ps.cur_page = a.nzp_page and ps.page_url = b.nzp_page) = 0;

--добавить недостающие пункты меню, у которых есть подменю
CREATE temp table ps (cur_page integer, up_kod integer);
insert into ps select distinct cur_page, up_kod from pages_show a where up_kod > 0 and not exists (select 1 from pages_show where cur_page = a.cur_page and page_url = a.up_kod);
insert into pages_show (cur_page, page_url, up_kod, sort_kod) select cur_page, ps.up_kod, 0, 0 from ps, pages Where ps.up_kod=pages.nzp_page;
drop table ps;


delete from pages_show where cur_page in (232,355,353,359,95,96,190,104,105,182,183,224,352, 359) or page_url in (232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_show WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM actions_lnk WHERE cur_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_actions WHERE  nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM role_pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);
DELETE FROM pages WHERE nzp_page in(232,355,353,359,95,96,190,104,105,182,183,224,352,359);

                                                                      --Обновление кодов сортировки для корректного отображения пунктов меню
                                                                  --Ћбновление кодов сортировки длЯ корректного отображениЯ пунктов меню
UPDATE pages_show SET sort_kod =3, up_kod = 0 WHERE page_url = 70;
UPDATE pages_show SET sort_kod =4, up_kod = 0 WHERE page_url = 254;
UPDATE pages_show SET sort_kod =5, up_kod = 0 WHERE page_url = 71;
UPDATE pages_show SET sort_kod =6, up_kod = 0 WHERE page_url = 235;
UPDATE pages_show SET sort_kod =7, up_kod = 0 WHERE page_url = 236;
UPDATE pages_show SET sort_kod =8, up_kod = 0 WHERE page_url = 275;
UPDATE pages_show SET sort_kod =9, up_kod = 0 WHERE page_url = 276;
UPDATE pages_show SET sort_kod =10, up_kod = 0 WHERE page_url = 78;
UPDATE pages_show SET sort_kod =11, up_kod = 0 WHERE page_url = 79;
UPDATE pages_show SET sort_kod =12, up_kod = 0 WHERE page_url = 80;
UPDATE pages_show SET sort_kod =13, up_kod = 0 WHERE page_url = 76;
UPDATE pages_show SET sort_kod =14, up_kod = 0 WHERE page_url = 74;
UPDATE pages_show SET sort_kod =15, up_kod = 0 WHERE page_url = 77;
UPDATE pages_show SET sort_kod =16, up_kod = 0 WHERE page_url = 75;
UPDATE pages_show SET sort_kod =17, up_kod = 0 WHERE page_url = 40;
UPDATE pages_show SET sort_kod =18, up_kod = 0 WHERE page_url = 30;
UPDATE pages_show SET sort_kod =19, up_kod = 0 WHERE page_url = 291;
UPDATE pages_show SET sort_kod =20, up_kod = 0 WHERE page_url = 150;
UPDATE pages_show SET sort_kod =21, up_kod = 0 WHERE page_url = 160;
UPDATE pages_show SET sort_kod =22, up_kod = 0 WHERE page_url = 357;
UPDATE pages_show SET sort_kod =23, up_kod = 0 WHERE page_url = 73;
UPDATE pages_show SET sort_kod =24, up_kod = 0 WHERE page_url = 72;

UPDATE pages_show SET sort_kod =1 WHERE page_url = 168;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 205;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 108;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 126;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 111;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 362;
UPDATE pages_show SET sort_kod =1 WHERE page_url = 41;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 193;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 50;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 170;
UPDATE pages_show SET sort_kod =2 WHERE page_url = 109;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 42;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 49;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 95;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 104;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 190;
UPDATE pages_show SET sort_kod =3 WHERE page_url = 117;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 53;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 182;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 118;
UPDATE pages_show SET sort_kod =4 WHERE page_url = 256;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 56;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 100;
UPDATE pages_show SET sort_kod =5 WHERE page_url = 355;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 352;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 197;
UPDATE pages_show SET sort_kod =6 WHERE page_url = 102;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 60;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 180;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 121;
UPDATE pages_show SET sort_kod =7 WHERE page_url = 99;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 110;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 65;
UPDATE pages_show SET sort_kod =8 WHERE page_url = 189;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 179;
UPDATE pages_show SET sort_kod =9 WHERE page_url = 238;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 196;
UPDATE pages_show SET sort_kod =10 WHERE page_url = 319;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 241;
UPDATE pages_show SET sort_kod =11 WHERE page_url = 354;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 7;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 263;
UPDATE pages_show SET sort_kod =12 WHERE page_url = 124;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 264;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 142;
UPDATE pages_show SET sort_kod =13 WHERE page_url = 98;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 184;
UPDATE pages_show SET sort_kod =14 WHERE page_url = 281;
UPDATE pages_show SET sort_kod =16 WHERE page_url = 57;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 116;
UPDATE pages_show SET sort_kod =19 WHERE page_url = 59;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 192;
UPDATE pages_show SET sort_kod =25 WHERE page_url = 240;
UPDATE pages_show SET sort_kod =30 WHERE page_url = 201;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 185;
UPDATE pages_show SET sort_kod =31 WHERE page_url = 51;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 54;
UPDATE pages_show SET sort_kod =37 WHERE page_url = 92;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 63;
UPDATE pages_show SET sort_kod =43 WHERE page_url = 66;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 186;
UPDATE pages_show SET sort_kod =49 WHERE page_url = 62;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 162;
UPDATE pages_show SET sort_kod =55 WHERE page_url = 67;
UPDATE pages_show SET sort_kod =56 WHERE page_url = 177;
UPDATE pages_show SET sort_kod =61 WHERE page_url = 55;
UPDATE pages_show SET sort_kod =62 WHERE page_url = 61;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 191;
UPDATE pages_show SET sort_kod =67 WHERE page_url = 165;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 262;
UPDATE pages_show SET sort_kod =73 WHERE page_url = 167;
UPDATE pages_show SET sort_kod =76 WHERE page_url = 158;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 347;
UPDATE pages_show SET sort_kod =79 WHERE page_url = 122;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 137;
UPDATE pages_show SET sort_kod =85 WHERE page_url = 166;
UPDATE pages_show SET sort_kod =91 WHERE page_url = 146;
UPDATE pages_show SET sort_kod =97 WHERE page_url = 296;
UPDATE pages_show SET sort_kod =103 WHERE page_url = 244;
UPDATE pages_show SET sort_kod =109 WHERE page_url = 123;
UPDATE pages_show SET sort_kod =115 WHERE page_url = 131;
UPDATE pages_show SET sort_kod =121 WHERE page_url = 85;
UPDATE pages_show SET sort_kod =127 WHERE page_url = 88;
UPDATE pages_show SET sort_kod =133 WHERE page_url = 97;
UPDATE pages_show SET sort_kod =139 WHERE page_url = 344;
UPDATE pages_show SET sort_kod =145 WHERE page_url = 345;
UPDATE pages_show SET sort_kod =151 WHERE page_url = 133;
UPDATE pages_show SET sort_kod =321 WHERE page_url = 9;
UPDATE pages_show SET sort_kod =338 WHERE page_url = 13;




DROP TRIGGER pages_add on pages; 
DROP FUNCTION add_to_pages();
DROP TRIGGER actions_show_add on actions_show; 
DROP FUNCTION add_to_actions_show(); 
DROP TRIGGER actions_lnk_add on actions_lnk; 
DROP FUNCTION add_to_actions_lnk(); 
DROP TRIGGER role_actions_add on role_actions; 
DROP FUNCTION add_to_role_actions(); 
DROP TRIGGER role_pages_add on role_pages; 
DROP FUNCTION add_to_role_pages(); 
DROP TRIGGER page_links_add on page_links; 
DROP FUNCTION add_to_page_links();
DROP TRIGGER s_actions_add on s_actions; 
DROP FUNCTION add_to_s_actions();  
DROP TRIGGER img_lnk_add on img_lnk; 
DROP FUNCTION add_to_img_lnk();
DROP TRIGGER roleskey_add on roleskey; 
DROP FUNCTION add_to_roleskey(); 
DROP TRIGGER s_roles_add on s_roles; 
DROP FUNCTION add_to_s_roles();
DROP TRIGGER report_add on report; 
DROP FUNCTION add_to_report(); 
  



update actions_show set sign = sort_kod::varchar||act_dd::varchar||act_tip::varchar||nzp_act::varchar||cur_page||'-'||nzp_ash||'actions_show'; 
update role_actions set sign = nzp_role::varchar||nzp_page::varchar||nzp_act::varchar||'-'||id::varchar||'role_actions' 
where nzp_role >= 10 and nzp_role < 1000; 
update roleskey set sign = tip::varchar||kod::varchar||nzp_role::varchar||'-'||nzp_rlsv::varchar||'roles' 
where (nzp_role >= 10 and nzp_role < 1000) or (tip = 105 and kod >= 10 and kod < 1000); 
update role_pages set sign = nzp_role::varchar||nzp_page::varchar||'-'||id::varchar||'role_pages' 
where nzp_role >= 10 and nzp_role < 1000; 
update pages_show set sign = sort_kod::varchar||up_kod::varchar||page_url||cur_page||'-'||nzp_psh||'pages_show';



--------------------------------Простановка координат для карты-------------------------------------
 drop FUNCTION if exists  public.SetMapCoordinates();
create or Replace function public.SetMapCoordinates()
returns text as 
$BODY$
DECLARE
nzp_mo int;
Begin
execute 'delete from public.map_objects where tip=-2';
execute 'insert into public.map_objects (tip) values(-2) returning nzp_mo ' into nzp_mo;
execute 'insert into  public.map_points (nzp_mo, x, y) values ('||nzp_mo||', 36.608076, 55.092617)';
return 'Выполнено';
end;
$BODY$
LANGUAGE plpgsql;
select public.SetMapCoordinates();
drop FUNCTION if exists  public.SetMapCoordinates();

--*****************************************************************************************************
drop function if exists public.table_exists (text, text);
create function public.table_exists(schm text, tbl text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.tables where table_schema = schm and table_name = tbl limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.column_exists (text, text, text);
create function public.column_exists(schm text, tbl text, clmn text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  select 1 into cnt from information_schema.columns where table_schema = schm and table_name = tbl and column_name = clmn limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_column (text, text, text, text);
create function public.create_column(schm text, tbl text, clmn text, clmn_type text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD ' || clmn || ' ' || clmn_type;
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.constraint_exists (text, text, text);
create function public.constraint_exists(schm text, tbl text, cnstr text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM information_schema.table_constraints WHERE table_schema = schm and table_name = tbl and upper(constraint_name) = upper(cnstr) limit 1; 
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_primary_key(text, text, text, text);
drop function if exists public.create_primary_key(text, text, text);
create function public.create_primary_key(schm text, tbl text, clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
 select 1 into cnt
  from information_schema.table_constraints t_c, information_schema.key_column_usage kcu 
  where t_c.constraint_type = 'PRIMARY KEY'
    and t_c.constraint_name = kcu.constraint_name
    and kcu.table_schema = lower(trim(schm)) 
    and t_c.table_schema = kcu.table_schema 
    and kcu.table_name = lower(trim(tbl)) 
    and t_c.table_name = kcu.table_name 
                and kcu.column_name = lower(trim(clmn)) limit 1;

                cnt := coalesce(cnt, 0);
  
  if cnt <= 0 then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || trim(tbl) || '_pkey PRIMARY KEY (' || clmn|| ')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_foreign_key(text, text, text, text, text, text, text);
drop function if exists public.create_foreign_key(text, text, text, text, text, text);
create function public.create_foreign_key(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
  cnstr text;
BEGIN
  cnstr := 'FK_' || tbl || '_' || clmn;
  if not public.constraint_exists(schm, tbl, cnstr) then
    EXECUTE 'alter table ' || schm || '.' || tbl || ' ADD CONSTRAINT ' || cnstr || ' FOREIGN KEY (' || clmn|| ') REFERENCES ' || ref_schema || '.' || ref_table || ' (' || ref_clmn || ')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.can_create_fk(text, text, text, text, text, text);
create function public.can_create_fk(schm text, tbl text, clmn text, ref_schema text, ref_table text, ref_clmn text) 
  RETURNS text AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  if not public.column_exists(schm, tbl, clmn) then
    return 'Выполнено';
  end if;
  
  EXECUTE 'select 1 from ' || schm || '.' || tbl || 
    ' where ' || clmn || ' not in (select ' || ref_clmn || ' from ' || ref_schema || '.' || ref_table || ' where ' || ref_clmn || ' is not null) 
      and ' || clmn || ' is not null ' into cnt; 
  cnt := coalesce(cnt, 0);
  
  if (cnt <> 0) then
    insert into public.agent_contract_error (err) values 
    ('В колонке ' || clmn || ' таблицы ' || schm || '.' || tbl || ' есть значения, которых нет в колонке ' || ref_clmn || ' таблицы ' || ref_schema || '.' || ref_table);
    return 'Выполнено';
  else
    return 'Выполнено';  
  end if;
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.index_exists (text, text, text);
create function public.index_exists(schm text, tbl text, indx text) 
  RETURNS bool AS
$BODY$
DECLARE
  cnt integer;
BEGIN
  SELECT 1 into cnt FROM pg_class c 
   JOIN pg_index i ON i.indexrelid = c.oid
   JOIN pg_class c2 ON i.indrelid = c2.oid
   LEFT JOIN pg_user u ON u.usesysid = c.relowner
   LEFT JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE c.relkind = 'i' 
   AND n.nspname = schm AND c2.relname = tbl AND upper(c.relname) = upper(indx) limit 1;
  cnt := coalesce(cnt, 0);
  return cnt <> 0;
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_index (text, text, text, text);
create function public.create_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

drop function if exists public.create_unique_index (text, text, text, text);
create function public.create_unique_index(schm text, tbl text, indx text, cols text) 
  RETURNS text AS
$BODY$
BEGIN
  if not public.index_exists(schm, tbl, indx) then
    EXECUTE 'CREATE UNIQUE INDEX ' || indx || ' ON ' || schm || '.' || tbl || '('|| cols ||')';
  end if;
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;


--*****************************************************************************************************
drop function if exists public.ac_check_fk(text);
create function public.ac_check_fk(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  drop table if exists public.agent_contract_error;
  create table public.agent_contract_error (err text);

  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  perform public.can_create_fk(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  perform public.can_create_fk(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  perform public.can_create_fk(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  perform public.can_create_fk(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  perform public.can_create_fk(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    perform public.can_create_fk(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.make_archive(text);
create function public.make_archive(pref text) returns text as 
$BODY$
BEGIN
  if not public.table_exists(pref || '_data', 'fn_bank_old') then
    EXECUTE 'create table ' || pref || '_data.fn_bank_old (LIKE ' || pref || '_data.fn_bank)';
    EXECUTE 'insert into ' || pref || '_data.fn_bank_old select * from ' || pref || '_data.fn_bank';
  end if;

  if not public.table_exists(pref || '_data', 'fn_dogovor_old') then
    EXECUTE 'create table ' || pref || '_data.fn_dogovor_old (LIKE ' || pref || '_data.fn_dogovor)';
    EXECUTE 'insert into ' || pref || '_data.fn_dogovor_old select * from ' || pref || '_data.fn_dogovor';
  end if;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_tables(text);
create function public.ac_create_tables(pref text) returns text as
$BODY$
DECLARE
  sdata text;
  kernel text;
  apref text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  -- fn_scope
  if not public.table_exists (sdata, 'fn_scope') then
    EXECUTE 'set search_path to ''' || sdata || '''';
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_scope_nzp_scope_seq INCREMENT 1 START 1;';
    EXECUTE 'CREATE TABLE fn_scope (nzp_scope integer DEFAULT nextval((''' || sdata || '.fn_scope_nzp_scope_seq''::text)::regclass) NOT NULL) WITH (OIDS=FALSE)'; 
  end if;
  
  -- fn_dogovor_bank_lnk
  if not public.table_exists(sdata, 'fn_dogovor_bank_lnk') then
    EXECUTE 'set search_path to ''' || sdata || '''';
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_dogovor_bank_lnk_id_seq INCREMENT 1 START 1';
    EXECUTE 'CREATE TABLE fn_dogovor_bank_lnk ( 
                  id integer DEFAULT nextval((''' || sdata || '.fn_dogovor_bank_lnk_id_seq''::text)::regclass) NOT NULL,
                  nzp_fd integer NOT NULL,
                  nzp_fb integer NOT NULL,
                  changed_by integer NOT NULL,
                  changed_on timestamp DEFAULT now() NOT NULL) WITH (OIDS=FALSE)';
  end if;

  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'priznak_perechisl', 'integer default 1');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'min_sum', 'numeric(13,2) default 0');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'max_sum', 'numeric(13,2) default 0');
  PERFORM public.create_column(sdata, 'fn_dogovor_bank_lnk', 'naznplat', 'varchar(1000)');
  
  -- fn_scope_adres  
  if not public.table_exists(sdata, 'fn_scope_adres') then
    EXECUTE 'CREATE SEQUENCE ' || sdata || '.fn_scope_adres_nzp_scope_adres_seq INCREMENT 1 START 1';
    execute 'CREATE TABLE fn_scope_adres ( 
                  nzp_scope_adres integer DEFAULT nextval((''' || sdata || '.fn_scope_adres_nzp_scope_adres_seq''::text)::regclass) NOT NULL,
                  nzp_scope integer NOT NULL,
                  nzp_wp integer NOT NULL,
                  nzp_town integer,
                  nzp_raj integer,
                  nzp_ul integer,
                  nzp_dom integer) WITH (OIDS=FALSE)';
  end if;
  
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  PERFORM public.create_column(sdata, 'fn_bank', 'note', 'varchar(1000)');
  
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_agent', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_payer_princip', 'integer');
  PERFORM public.create_column(sdata, 'fn_dogovor', 'nzp_scope', 'integer');
  
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_by', 'integer');
  PERFORM public.create_column(sdata, 'fn_scope_adres', 'changed_on', 'timestamp DEFAULT now() NOT NULL');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_payer_podr', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'dpd', 'smallint DEFAULT 0'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'nzp_scope', 'integer'); 
    PERFORM public.create_column(apref || '_kernel', 'supplier', 'changed_on', 'timestamp DEFAULT now() NOT NULL');

    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN adres_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN phone_supp DROP NOT NULL';
    EXECUTE 'alter table ' || apref || '_kernel.supplier ALTER COLUMN geton_plat DROP NOT NULL';
  end loop;
  
  -- drop not null
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN num_count DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bank_name DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN kcount DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN bik DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_bank ALTER COLUMN npunkt DROP NOT NULL';

  -- s_payer
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN nzp_type DROP NOT NULL';
  EXECUTE 'alter table ' || kernel || '.s_payer ALTER COLUMN changed_on SET DEFAULT now()';

  -- payer_types
  EXECUTE 'alter table ' || kernel || '.payer_types SET WITHOUT OIDS';
  EXECUTE 'alter table ' || kernel || '.payer_types ALTER COLUMN changed_on SET DEFAULT now()';
  
  -- fn_dogovor
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_area DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer_ar DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_fb DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_osnov DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN kpp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN max_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN priznak_perechisl DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN min_sum DROP NOT NULL ';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN naznplat DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_supp DROP NOT NULL';
  EXECUTE 'alter table ' || sdata || '.fn_dogovor ALTER COLUMN nzp_payer DROP NOT NULL';
  
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_indexes(text);
create function public.ac_create_indexes(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_index(kernel, 's_payer', 'ix_s_payer_changed_by', 'changed_by');

  PERFORM public.create_index(kernel, 'payer_types', 'ix_payer_types_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_scope', 'IX_fn_scope_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope', 'ix_fn_scope_changed_by', 'changed_by');

  PERFORM public.create_unique_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_id', 'id');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fd', 'nzp_fd');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_nzp_fb', 'nzp_fb');
  PERFORM public.create_index(sdata, 'fn_dogovor_bank_lnk', 'IX_fn_dogovor_bank_lnk_changed_by', 'changed_by');

  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer', 'nzp_payer');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_payer_bank', 'nzp_payer_bank');
  PERFORM public.create_index(sdata, 'fn_bank', 'IX_fn_bank_nzp_user', 'nzp_user');

  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_agent', 'nzp_payer_agent');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_payer_princip', 'nzp_payer_princip');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_user', 'nzp_user');
  PERFORM public.create_index(sdata, 'fn_dogovor', 'IX_fn_dogovor_nzp_scope', 'nzp_scope');

  PERFORM public.create_unique_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope_adres', 'nzp_scope_adres');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_scope', 'nzp_scope');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_wp', 'nzp_wp');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_town', 'nzp_town');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_raj', 'nzp_raj');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_ul', 'nzp_ul');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'IX_fn_scope_adres_nzp_dom', 'nzp_dom');
  PERFORM public.create_index(sdata, 'fn_scope_adres', 'ix_fn_scope_adres_changed_by', 'changed_by');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_agent', 'nzp_payer_agent');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_princip', 'nzp_payer_princip');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_podr', 'nzp_payer_podr');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_payer_supp', 'nzp_payer_supp');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_fn_dogovor_bank_lnk_id', 'fn_dogovor_bank_lnk_id');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_nzp_scope', 'nzp_scope');
    PERFORM public.create_index(apref || '_kernel', 'supplier', 'IX_supplier_changed_by', 'changed_by');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_primary_keys(text);
create function public.ac_create_primary_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_primary_key(sdata, 'users', 'nzp_user'); 
  PERFORM public.create_primary_key(kernel, 's_point', 'nzp_wp');  
  PERFORM public.create_primary_key(sdata, 's_town', 'nzp_town'); 
  PERFORM public.create_primary_key(sdata, 's_rajon', 'nzp_raj'); 
  PERFORM public.create_primary_key(sdata, 's_ulica', 'nzp_ul'); 
  PERFORM public.create_primary_key(sdata, 'dom', 'nzp_dom'); 
  
  PERFORM public.create_primary_key(kernel, 's_payer_types', 'nzp_payer_type'); 
  PERFORM public.create_primary_key(kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_primary_key(kernel, 'payer_types', 'nzp_pt');
  PERFORM public.create_primary_key(sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor_bank_lnk', 'id');
  PERFORM public.create_primary_key(sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_primary_key(sdata, 'fn_dogovor', 'nzp_fd');  
  PERFORM public.create_primary_key(sdata, 'fn_scope_adres', 'nzp_scope_adres');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_primary_key(apref || '_kernel', 'supplier', 'nzp_supp');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_create_foreign_keys(text);
create function public.ac_create_foreign_keys(pref text) returns text as
$BODY$
DECLARE
  sdata  text;
  kernel text;
  apref  text;
BEGIN
  sdata := pref || '_data';
  kernel := pref || '_kernel';
  
  PERFORM public.create_foreign_key(kernel, 's_payer', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'nzp_payer_type', kernel, 's_payer_types', 'nzp_payer_type');
  PERFORM public.create_foreign_key(kernel, 'payer_types', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fb', sdata, 'fn_bank', 'nzp_fb');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'nzp_fd', sdata, 'fn_dogovor', 'nzp_fd');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor_bank_lnk', 'changed_by', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_payer_bank', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_bank', 'nzp_user', sdata, 'users', 'nzp_user');
  
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_user', sdata, 'users', 'nzp_user');
  PERFORM public.create_foreign_key(sdata, 'fn_dogovor', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_dom', sdata, 'dom', 'nzp_dom');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_raj', sdata, 's_rajon', 'nzp_raj');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_town', sdata, 's_town', 'nzp_town');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_ul', sdata, 's_ulica', 'nzp_ul');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'nzp_wp', kernel, 's_point', 'nzp_wp');
  PERFORM public.create_foreign_key(sdata, 'fn_scope_adres', 'changed_by', sdata, 'users', 'nzp_user');
  
  EXECUTE 'set search_path to ' || kernel;
  
  for apref in select trim(bd_kernel) from s_point order by 1
  loop
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'fn_dogovor_bank_lnk_id', sdata, 'fn_dogovor_bank_lnk', 'id');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_agent', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_podr', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_princip', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_payer_supp', kernel, 's_payer', 'nzp_payer');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'changed_by', sdata, 'users', 'nzp_user');
    PERFORM public.create_foreign_key(apref || '_kernel', 'supplier', 'nzp_scope', sdata, 'fn_scope', 'nzp_scope');
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.ac_prepare_data(text);
create function public.ac_prepare_data(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  cnt integer;
  apref text;
  anzp_fd integer;
  anzp_supp integer;
  anzp_wp integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';

  -- тип подрядчик
  EXECUTE 'select count(*) from ' || kernel || '.s_payer_types where nzp_payer_type = 11' into cnt;
  if (cnt = 0) then
    EXECUTE 'insert into ' || kernel || '.s_payer_types (nzp_payer_type, type_name, is_system) values (11, ''Подрядчик'', 1)'; 
  end if;
  
  -- проставить БИК и расчетные счета
  EXECUTE 'update ' || kernel || '.s_payer s set 
      bik = (select max(trim(bik))   from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank), 
      ks = (select max(trim(kcount)) from ' || sdata || '.fn_bank b where s.nzp_payer = b.nzp_payer_bank)
    where s.nzp_payer in (select nzp_payer_bank from ' || sdata || '.fn_bank)';
  
  -- для банков довставить типы
  EXECUTE 'insert into ' || kernel || '.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on) 
   select distinct nzp_payer, 8, 1, now() from ' || sdata || '.fn_bank where nzp_payer not in (select nzp_payer from ' || kernel || '.payer_types where nzp_payer_type = 8)';
  
  -- принципалы
execute 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_princip, 10 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=10 and s.nzp_payer_princip=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_princip,0) > 0';

-- агенты
execute 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_agent, 5 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=5 and s.nzp_payer_agent=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_agent,0) >0';

--поставщики
 EXECUTE 'insert into '||kernel||'.payer_types (nzp_payer, nzp_payer_type, changed_by, changed_on)
SELECT DISTINCT nzp_payer_supp, 2 , 1 , now() from '||kernel||'.supplier s where not exists 
(select nzp_payer from '||kernel||'.payer_types pt where nzp_payer_type=2 and s.nzp_payer_supp=pt.nzp_payer and coalesce(pt.nzp_payer,0)>0 ) 
and coalesce(nzp_payer_supp,0) >0';

  -- fn_bank
  EXECUTE 'select count(*) from ' || kernel || '.s_payer where nzp_payer = -999999999' into cnt;
  if (cnt = 0) then
    EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) values (-999999999, ''Банк не определен'', ''Банк не определен'')'; 
  end if;
  
  execute 'insert into '||sdata||'.fn_bank (nzp_payer,num_count, bank_name, rcount, kcount, bik,nzp_user, dat_when, nzp_payer_bank)
select distinct nzp_payer_princip, 0, ''Банк не определен'', ''00000000000000000000'',''00000000000000000000'',''000000000'', 1, now(), -999999999
from '||kernel||'.supplier s 
where not exists (select 1 from '||sdata||'.fn_bank b where s.nzp_payer_princip=b.nzp_payer) 
 and s.nzp_payer_princip is not null
ORDER BY nzp_payer_princip';
 
  -- fn_dogovor
  -- nzp_payer_agent
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_agent = (select max(s.nzp_payer_agent) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_agent is not null) 
                where d.nzp_payer_agent is null';
  
  -- nzp_payer_princip
  EXECUTE 'update ' || sdata || '.fn_dogovor d set
    nzp_payer_princip = (select max(s.nzp_payer_princip) from ' || kernel || '.supplier s where s.nzp_supp = d.nzp_supp and s.nzp_payer_princip is not null) 
                where d.nzp_payer_princip is null';

  -- довставить агентов и принципалов              
  EXECUTE 'insert into ' || sdata || '.fn_dogovor (nzp_payer_agent, nzp_payer_princip, nzp_supp, nzp_user, dat_when)
    select nzp_payer_agent, nzp_payer_princip, max(s.nzp_supp), 1, now()
    from ' || kernel || '.supplier s
    where s.nzp_payer_princip is not null 
                  and not exists (select 1 from ' || sdata || '.fn_dogovor d
                    where d.nzp_payer_agent = s.nzp_payer_agent and d.nzp_payer_princip = s.nzp_payer_princip and s.nzp_payer_agent is not null and s.nzp_payer_princip is not null)
                group by 1,2';
  
  
  -- определить области действия договоров ЖКУ
  execute 'update ' || sdata || '.fn_dogovor set nzp_scope = null';
  EXECUTE 'set search_path to ' || kernel;  
  for apref in 
     select trim(bd_kernel)p from s_point order by 1
  loop
    execute 'update ' || apref || '_kernel.supplier set nzp_scope = null;';  
  end loop;
  
  execute 'delete from ' || sdata || '.fn_scope_adres';
  execute 'delete from ' || sdata || '.fn_scope';
  
  drop table if exists tmp_supp_wp;
  Create temp table tmp_supp_wp (nzp_supp integer, nzp_wp integer, nzp_scope integer);

  EXECUTE 'set search_path to ' || kernel;  
  for apref, anzp_wp in 
     select trim(bd_kernel), nzp_wp from s_point where flag > 1 order by 1
  loop
      execute 'insert into tmp_supp_wp (nzp_wp, nzp_supp) 
         select distinct '||anzp_wp||', nzp_supp from  '|| apref || '_data.tarif t 
         where coalesce(nzp_supp,0)> 0 and is_actual = 1';
  end loop;
   
  EXECUTE 'select max(nzp_scope) from ' || sdata || '.fn_scope' into cnt ;
  cnt := coalesce(cnt, 1);  
  
  EXECUTE 'set search_path to ' || sdata;  
  for anzp_supp in 
    select distinct nzp_supp from tmp_supp_wp
  loop
    cnt := cnt + 1;
    insert into fn_scope(nzp_scope, changed_by, changed_on) values (cnt, 1, now());
    update tmp_supp_wp set nzp_scope = cnt where nzp_supp = anzp_supp;	
  end loop;
  EXECUTE 'ALTER SEQUENCE fn_scope_nzp_scope_seq RESTART with ' || cnt;  
  
  insert into fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on)  select distinct nzp_scope, nzp_wp, 1, now() from tmp_supp_wp;

  -- определить область действия договоров ЕРЦ
  EXECUTE 'select max(nzp_scope) from ' || sdata || '.fn_scope' into cnt ;
  cnt := coalesce(cnt, 1);  
  EXECUTE 'set search_path to ' || sdata;  
  
  for anzp_fd in 
    select nzp_fd from fn_dogovor 
    where nzp_payer_agent is not null and nzp_payer_princip is not null
  loop
    cnt := cnt + 1;
    insert into fn_scope(nzp_scope, changed_by, changed_on) values (cnt, 1, now());
    update fn_dogovor set nzp_scope = cnt where nzp_fd = anzp_fd;
  end loop;
  EXECUTE 'ALTER SEQUENCE fn_scope_nzp_scope_seq RESTART with ' || cnt;
  
  execute 'insert into ' || sdata || '.fn_scope_adres (nzp_scope, nzp_wp, changed_by, changed_on) 
  select distinct d.nzp_scope, t.nzp_wp, 1, now()
  from ' || sdata || '.fn_dogovor d, ' || kernel || '.supplier s, tmp_supp_wp t
  where d.nzp_payer_agent = s.nzp_payer_agent 
    and d.nzp_payer_princip = s.nzp_payer_princip 
    and t.nzp_supp = s.nzp_supp '; 
    
  -- fn_dogovor_bank_lnk
  EXECUTE 'set search_path to ' || sdata; 
  insert into fn_dogovor_bank_lnk (nzp_fd, nzp_fb, changed_by, changed_on)
    select a.nzp_fd, max(b.nzp_fb), 1, now() 
  from fn_dogovor a, fn_bank b 
    where b.nzp_payer = a.nzp_payer_princip and not exists (select 1 from fn_dogovor_bank_lnk l where l.nzp_fd = a.nzp_fd and l.nzp_fb = b.nzp_fb)
  group by 1;
  
  -- supplier
  EXECUTE 'set search_path to ' || kernel; 
  for apref in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'update ' || apref || '_kernel.supplier s set 
     fn_dogovor_bank_lnk_id = (select max(l.id)
     from ' || sdata || '.fn_dogovor d, ' || sdata || '.fn_dogovor_bank_lnk l
       where s.nzp_payer_agent = d.nzp_payer_agent
         and s.nzp_payer_princip = d.nzp_payer_princip
         and d.nzp_fd = l.nzp_fd)
     where s.fn_dogovor_bank_lnk_id is null';
				
     EXECUTE 'update ' || apref || '_kernel.supplier s set 
       nzp_scope = (select max(l.nzp_scope) from tmp_supp_wp l where s.nzp_supp = l.nzp_supp) 
       where s.nzp_scope is null';			
  end loop;

  EXECUTE 'update ' || sdata || '.fn_dogovor_bank_lnk  f set naznplat = (select max(naznplat) from ' || sdata || '.fn_dogovor where nzp_fd = f.nzp_fd)';

  return 'Выполнено'; 
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.before_check_fk(text);
create function public.before_check_fk(pref text) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  apref text;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';

  -- почистить кривые ссылки в payer_types
  execute 'delete from ' || kernel || '.payer_types where nzp_payer not in (select nzp_payer from ' || kernel || '.s_payer)';
  execute 'delete from ' || kernel || '.payer_types where nzp_payer_type not in (select nzp_payer_type from ' || kernel || '.s_payer_types)';
  
  -- почистить кривые ссылки в fn_bank
  execute 'delete from ' || sdata || '.fn_bank where nzp_payer      not in (select nzp_payer from ' || kernel || '.s_payer)';
  execute 'delete from ' || sdata || '.fn_bank where nzp_payer_bank not in (select nzp_payer from ' || kernel || '.s_payer)';
  
  -- добавить контрагентов из supplier
  EXECUTE 'set search_path to ' || kernel; 
  for apref in select trim(bd_kernel) from s_point order by 1 
  loop
	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_agent, ''Поставщик не определен '' || nzp_payer_agent, ''Поставщик не определен '' || nzp_payer_agent 
	  from ' || apref || '_kernel.supplier where nzp_payer_agent not in (select nzp_payer from ' || kernel || '.s_payer)'; 
	
	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_princip, ''Поставщик не определен '' || nzp_payer_princip, ''Поставщик не определен '' || nzp_payer_princip 
	  from ' || apref || '_kernel.supplier where nzp_payer_princip not in (select nzp_payer from ' || kernel || '.s_payer)'; 

	EXECUTE 'insert into ' || kernel || '.s_payer (nzp_payer, payer, npayer) 
	  select distinct nzp_payer_supp, ''Поставщик не определен '' || nzp_payer_supp, ''Поставщик не определен '' || nzp_payer_supp 
	  from ' || apref || '_kernel.supplier where nzp_payer_supp not in (select nzp_payer from ' || kernel || '.s_payer)'; 
  end loop;

  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;

--*****************************************************************************************************
drop function if exists public.agent_fin(text, integer, integer);
create function public.agent_fin(pref text, in_year_from integer, in_year_to integer) returns text as
$BODY$
DECLARE
  fin text;
  sdata text;
  i integer;
BEGIN
  i := in_year_from;
  sdata := pref || '_data';
  
  while i <= in_year_to loop
    fin := pref || '_fin_' || i; 
    execute 'alter table ' || fin || '.fn_sended_dom alter column nzp_fd DROP NOT NULL';
    PERFORM public.create_column(fin, 'fn_sended_dom', 'fn_dogovor_bank_lnk_id', 'integer');

    PERFORM public.create_column(fin, 'fn_sended', 'naznplat', 'varchar(1000)');
    PERFORM public.create_column(fin, 'fn_sended', 'fn_dogovor_bank_lnk_id', 'integer');
    PERFORM public.create_column(fin, 'fn_sended', 'nzp_fd', 'integer');
	
    execute 'update ' || fin || '.fn_sended f set naznplat = (select max(a.naznplat) from ' || sdata || '.fn_dogovor a where a.nzp_fd = f.nzp_fd)';
    i := i + 1;
  end loop;
  
  return 'Выполнено';
END;
$BODY$
LANGUAGE plpgsql;
--*****************************************************************************************************

drop function if exists public.agent_contract(text);
drop function if exists public.agent_contract(text, integer, integer);

create function public.agent_contract(pref text, in_year_from integer, in_year_to integer) returns text as
$BODY$
DECLARE
  kernel text;
  sdata text;
  aerr text;
  cnt integer;
  anzp_fd integer;
  localbank text;
  is_tula integer;
BEGIN
  kernel := pref || '_kernel';
  sdata := pref || '_data';
  
  -- поправить кривые ссылки
  PERFORM public.before_check_fk(pref); 
  
  PERFORM public.ac_check_fk(pref);
  
  select count(*) into cnt from public.agent_contract_error;
  
  if  cnt <> 0 then
    RAISE NOTICE 'Помойка в данных';

    for aerr in select err from public.agent_contract_error
    loop
      RAISE NOTICE '%', aerr;
    end loop;
    drop table if exists public.agent_contract_error;

    return 'Не выполнено. См. сообщения';       
  end if;

  perform public.agent_fin(pref, in_year_from, in_year_to);
  
  -- сделать архив для таблиц fn_bank и fn_dogovor
  RAISE NOTICE 'Создание архива таблиц fn_bank и fn_dogovor'; 
  perform public.make_archive(pref);
  RAISE NOTICE 'Архив таблиц fn_bank и fn_dogovor создан'; 
  
  RAISE NOTICE 'Создание таблиц'; 
  PERFORM public.ac_create_tables(pref);
  RAISE NOTICE 'Таблицы созданы'; 

  RAISE NOTICE 'Создание индексов'; 
  PERFORM public.ac_create_indexes(pref);
  RAISE NOTICE 'Индексы созданы'; 

  RAISE NOTICE 'Создание первичных ключей'; 
  PERFORM public.ac_create_primary_keys(pref);
  RAISE NOTICE 'Первичные ключи созданы'; 

  RAISE NOTICE 'Создание внешних ключей'; 
  PERFORM public.ac_create_foreign_keys(pref);
  RAISE NOTICE 'Внешние ключи созданы'; 

  EXECUTE 'drop function if exists ' || kernel || '.trigger_supplier() CASCADE;';

  RAISE NOTICE 'Приведение данных'; 
  PERFORM public.ac_prepare_data(pref);
  RAISE NOTICE 'Данные приведены'; 
  
  RAISE NOTICE 'Создание триггеров'; 
  
  EXECUTE 'CREATE function ' || kernel || '.trigger_supplier() RETURNS trigger AS 
$trigger_supplier$
DECLARE
  fn_dogovor_nzp_payer_agent integer;
  fn_dogovor_nzp_payer_princip integer;
BEGIN
  select nzp_payer_agent, nzp_payer_princip into fn_dogovor_nzp_payer_agent, fn_dogovor_nzp_payer_princip 
  from ' || sdata || '.fn_dogovor_bank_lnk l, ' || sdata || '.fn_dogovor d
  where l.nzp_fd = d.nzp_fd
    and l.id = NEW.fn_dogovor_bank_lnk_id;

  if NEW.nzp_payer_agent <> fn_dogovor_nzp_payer_agent 
    and NEW.nzp_payer_agent > 0         and fn_dogovor_nzp_payer_agent > 0
    and NEW.nzp_payer_agent is not null and fn_dogovor_nzp_payer_agent is not null
  then
    RAISE EXCEPTION ''Платежный агент договора ЖКУ не соответствует платежному агента договора ЕРЦ'';
  end if;

  if NEW.nzp_payer_princip <> fn_dogovor_nzp_payer_princip 
    and NEW.nzp_payer_princip > 0          and fn_dogovor_nzp_payer_princip > 0
    and NEW.nzp_payer_princip is not null  and fn_dogovor_nzp_payer_princip is not null
  then
    RAISE EXCEPTION ''Принципал договора ЖКУ не соответствует принципалу договора ЕРЦ'';
  end if;  

  return NEW;
END;
$trigger_supplier$
LANGUAGE  plpgsql;';

  EXECUTE 'set search_path to ' || kernel; 
  for localbank in select trim(bd_kernel) from s_point order by 1 
  loop
    EXECUTE 'CREATE TRIGGER ins_supplier BEFORE INSERT ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
    EXECUTE 'CREATE TRIGGER upd_supplier BEFORE UPDATE ON ' || localbank || '_kernel.supplier FOR EACH ROW EXECUTE PROCEDURE ' || kernel || '.trigger_supplier()';
  end loop;

  RAISE NOTICE 'Триггеры созданы';

  return ' ';
END;
$BODY$
LANGUAGE plpgsql;

-- главный вызов
select public.agent_contract('fbill', 13, 15);

drop table if exists fbill_data.nzpfd_general;
drop table if exists fbill_data.nzpfd_good;
-- общая временная таблица для связи между 
create table fbill_data.nzpfd_general (
nzp_fd integer,
nzp_fd_del integer,
dat_dog timestamp,
num_dog char(100),
nzp_payer_agent int,
nzp_fb integer,
nzp_payer_princip int);
-- временная таблица для nzp_fd, которые остануться
create table fbill_data.nzpfd_good (
nzp_fd integer,
nzp_fd_del integer,
dat_dog timestamp,
num_dog char(100),
nzp_payer_agent int,
nzp_payer_princip int);
-- выберем в таблицу fbill_data.nzpfd_general те nzp_fd, которые будут удалены
insert into fbill_data.nzpfd_general (nzp_fd_del, dat_dog, num_dog, nzp_payer_agent, nzp_payer_princip)
select nzp_fd, dat_dog,num_dog, nzp_payer_agent, nzp_payer_princip  from fbill_data.fn_dogovor 
where nzp_fd not in (select min(nzp_fd) from fbill_data.fn_dogovor group by nzp_payer_agent ,nzp_payer_princip, dat_dog, num_dog)
order by 4,5;
-- выберем в таблицу fbill_data.nzpfd_good те nzp_fd, которые остануться 
insert into fbill_data.nzpfd_good (nzp_fd, dat_dog, num_dog, nzp_payer_agent, nzp_payer_princip)
select nzp_fd, dat_dog,num_dog, nzp_payer_agent, nzp_payer_princip 
from fbill_data.fn_dogovor where nzp_fd  in (select min(nzp_fd) from fbill_data.fn_dogovor group by nzp_payer_agent ,nzp_payer_princip, dat_dog, num_dog)
order by 4,5;
-- обновим в таблице fbill_data.nzpfd_general те nzp_fd, которые останутся
update fbill_data.nzpfd_general t set nzp_fd= (select nzp_fd from fbill_data.nzpfd_good f 
 where t.nzp_payer_agent=f.nzp_payer_agent and t.nzp_payer_princip=f.nzp_payer_princip and coalesce(t.num_dog,'x')=coalesce(f.num_dog,'x') and 
 coalesce(t.dat_dog,'2015-06-25')=coalesce(f.dat_dog,'2015-06-25'));
-- посмотрим что добавилось
select * from fbill_data.nzpfd_general;
select * from fbill_data.nzpfd_good order by nzp_fd;

-- посмотрим на те договора, которые будут удалены
select * from fbill_data.fn_dogovor  where nzp_fd in ( select nzp_fd_del from fbill_data.nzpfd_general order by nzp_fd, nzp_fd_del);

--обновим в fbill_data.fn_dogovor те договоры, у которых nzp_fb = -1      -- и которые будут удаляться
update fbill_data.fn_dogovor f set nzp_fb = (select max(nzp_fb) from fbill_data.fn_bank where f.nzp_payer_princip = nzp_payer)
where nzp_fb = -1; --and nzp_fd in ( select nzp_fd_del from fbill_data.nzpfd_general order by nzp_fd, nzp_fd_del)

-- обновить все fn_sended_dom и fn_sended
drop FUNCTION if exists public.updateFnSended();
create or Replace function public.updateFnSended()
returns text as 
$BODY$
DECLARE
isExists boolean;
schem text;
table_name text;
req text;
begin
FOR i IN 11..15 LOOP
schem='fbill_fin_'||i;
-- обновление fn_sended
req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended'' and table_schema='''||schem||''')';
EXECUTE req into isExists;
if isExists  then
-- обновление  fn_dogovor_bank_lnk_id
--RAISE NOTICE '%',req;
	req='update '||schem||'.fn_sended s set fn_dogovor_bank_lnk_id = 
(select max(id) from fbill_data.fn_dogovor_bank_lnk l where 
s.nzp_fd = l.nzp_fd and l.nzp_fb in (select nzp_fb from fbill_data.fn_dogovor f  where l.nzp_fd = f.nzp_fd))
where fn_dogovor_bank_lnk_id is not null';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;

-- обновление fn_sended_dom
	req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended_dom'' and table_schema='''||schem||''')';
	EXECUTE req into isExists;
if isExists  then
	--RAISE NOTICE '%',req;
	req='update '||schem||'.fn_sended_dom d set fn_dogovor_bank_lnk_id= (select fn_dogovor_bank_lnk_id from '||schem||'.fn_sended s where s.nzp_snd=d.nzp_send)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;
END LOOP;
  return ''; 
end;
$BODY$
LANGUAGE plpgsql;
select * from public.updateFnSended();
drop FUNCTION if exists public.updateFnSended();

-- обновить fn_dogovor_bank_lnk_id
update fbill_data.fn_dogovor_bank_lnk l set nzp_fd = (select nzp_fd from fbill_data.nzpfd_general where nzp_fd_del = l.nzp_fd)
where nzp_fd in (select nzp_fd_del from fbill_data.nzpfd_general);


-- обновить все fn_sended_dom и fn_sended
drop FUNCTION if exists public.updateFnSendedDom();
create or Replace function public.updateFnSendedDom()
returns text as 
$BODY$
DECLARE
isExists boolean;
schem text;
table_name text;
req text;
begin
FOR i IN 11..15 LOOP
schem='fbill_fin_'||i;
-- обновление fn_sended
req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended'' and table_schema='''||schem||''')';
EXECUTE req into isExists;
if isExists  then
	-- обновление nzp_fd
	req='update '||schem||'.fn_sended s set nzp_fd=(select nzp_fd from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)
where exists (select 1 from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;

-- обновление fn_sended_dom
	req='select exists (SELECT 1 FROM INFORMATION_SCHEMA.tables WHERE table_name=''fn_sended_dom'' and table_schema='''||schem||''')';
	EXECUTE req into isExists;
if isExists  then
-- обновление nzp_fd
	req='update '||schem||'.fn_sended_dom s set nzp_fd=(select nzp_fd from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)
	where exists (select 1 from fbill_data.nzpfd_general g where s.nzp_fd=g.nzp_fd_del)';
	EXECUTE req;
	--RAISE NOTICE '%',req;
end if;
END LOOP;
  return ''; 
end;
$BODY$
LANGUAGE plpgsql;
select * from public.updateFnSendedDom();
drop FUNCTION if exists public.updateFnSendedDom();

-- удалить дубликаты 
delete from fbill_data.fn_dogovor where nzp_fd in (select nzp_fd_del from fbill_data.nzpfd_general);

drop table if exists fbill_data.nzpfd_general;
drop table if exists fbill_data.nzpfd_good;

--SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE   Column_Name = 'nzp_fd'

INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1985', 'Пустой расчет', NULL, NULL, NULL, '8', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1985);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1986', 'Пустой расчет', NULL, NULL, NULL, '6', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1986);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1987', 'Пустой расчет', NULL, NULL, NULL, '2', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1987);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1988', 'Пустой расчет', NULL, NULL, NULL, '5', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1988);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1989', 'Пустой расчет', NULL, NULL, NULL, '4', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1989);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device")
SELECT '1990', 'Пустой расчет', NULL, NULL, NULL, '1', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1990);
INSERT INTO "fbill_kernel"."formuls" ("nzp_frm", "name_frm", "dat_s", "dat_po", "tarif", "nzp_measure", "is_device") 
SELECT '1991', 'Пустой расчет', NULL, NULL, NULL, '3', '0' WHERE not exists (SELECT 1 FROM "fbill_kernel"."formuls"  WHERE nzp_frm=1991);



select 'Выполнено' as Результат;