grant dba to updb;
grant dba to public;

----------------Создание БД для Программы иниц. обновление----------------------------------------
CREATE DATABASE updater;
--Таблица ip районов
CREATE TABLE updb.rajon_ip
(rajon_number INT,
 rajon_name char(50),
 rajon_ip char(30));
 
 --История обновлений для каждого района
CREATE TABLE updb.rajon_history
(rajon_name char(50),
 update_status char(5),
 update_type char(20),
 update_version char(50),
 update_date datetime year to minute,
 update_report TEXT);
 
 -------------Заполнение данными таб. rajon_ip--------------------------
INSERT INTO updb.rajon_ip VALUES (0,"STCLINE", "www.stcline.ru:81");
INSERT INTO updb.rajon_ip VALUES (1,"Тест", "192.168.1.102:80");
INSERT INTO updb.rajon_ip VALUES (2,"Лаишево", "178.205.133.45:80");
INSERT INTO updb.rajon_ip VALUES (3,"Нижнекамск", "178.206.227.10:80");
INSERT INTO updb.rajon_ip VALUES (4,"Мензелинск", "178.206.227.70:80");
INSERT INTO updb.rajon_ip VALUES (5,"Лениногорск", "217.23.188.14:80");
INSERT INTO updb.rajon_ip VALUES (6,"Высокогорский", "178.205.135.30:80");
INSERT INTO updb.rajon_ip VALUES (7,"Кукморский", "178.205.133.205:80");
INSERT INTO updb.rajon_ip VALUES (8,"Алексеевский", "178.204.136.141:80");
INSERT INTO updb.rajon_ip VALUES (9,"Арский", "178.205.131.127:80");
INSERT INTO updb.rajon_ip VALUES (10,"Новошешминский", "178.205.135.13:80");
INSERT INTO updb.rajon_ip VALUES (11,"Актанышский", "178.206.227.125:80");
INSERT INTO updb.rajon_ip VALUES (12,"Буинский", "178.205.135.146:80");
INSERT INTO updb.rajon_ip VALUES (13,"Спасский", "178.205.133.140:80");
INSERT INTO updb.rajon_ip VALUES (14,"Техноконсалтинг", "81.25.173.44:80");
INSERT INTO updb.rajon_ip VALUES (15,"Камское Устье", "178.205.134.209:80");
INSERT INTO updb.rajon_ip VALUES (16,"Бавлы (Наш дом)", "178.207.11.4:80");
INSERT INTO updb.rajon_ip VALUES (17,"Бугульма", "178.207.8.241:80");
INSERT INTO updb.rajon_ip VALUES (18,"Бавлы", "178.207.8.176:80");
INSERT INTO updb.rajon_ip VALUES (19,"Нурлат", "178.205.133.98:80");
INSERT INTO updb.rajon_ip VALUES (20,"Заинск", "178.206.227.17:80");
INSERT INTO updb.rajon_ip VALUES (21,"Рыбная слобода", "178.205.133.224:80");
INSERT INTO updb.rajon_ip VALUES (22,"Альметьевск", "217.173.19.69:80");
INSERT INTO updb.rajon_ip VALUES (23,"Тукаевский", "178.206.227.118:80");
INSERT INTO updb.rajon_ip VALUES (24,"Балтаси", "178.205.133.168:80");
INSERT INTO updb.rajon_ip VALUES (25,"Черемшан", "178.207.11.104:80");
INSERT INTO updb.rajon_ip VALUES (26,"Мамадыш", "78.138.172.247:80");